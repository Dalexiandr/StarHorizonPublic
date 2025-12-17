import typing
import logging

from pydash import py_

from file import FluentFile
from fluentast import FluentAstAbstract
from fluentformatter import FluentFormatter
from project import Project
from fluent.syntax import ast, FluentParser, FluentSerializer


class RelativeFile:
    def __init__(self, file: FluentFile, locale: typing.AnyStr, relative_path_from_locale: typing.AnyStr):
        self.file = file
        self.locale = locale
        self.relative_path_from_locale = relative_path_from_locale


class FilesFinder:
    def __init__(self, project: Project):
        self.project: Project = project
        self.created_files: typing.List[FluentFile] = []

    def get_relative_path_dict(self, file: FluentFile, locale):
        base_path = self.project.ru_locale_dir_path if locale == 'ru-RU' else self.project.en_locale_dir_path
        return RelativeFile(
            file=file,
            locale=locale,
            relative_path_from_locale=file.get_relative_path(base_path)
        )

    def get_file_pair(self, en_file: FluentFile) -> typing.Tuple[FluentFile, FluentFile]:
        ru_file_path = en_file.full_path.replace('en-US', 'ru-RU')
        ru_file = FluentFile(ru_file_path)

        return en_file, ru_file

    def execute(self):
        self.created_files = []
        groups = self.get_files_pars()
        keys_without_pair = list(filter(lambda g: len(groups[g]) < 2, groups))

        for key in keys_without_pair:
            relative_file: RelativeFile = groups[key][0]

            if relative_file.locale == 'en-US':
                ru_file = self.create_ru_analog(relative_file)
                self.created_files.append(ru_file)
            elif relative_file.locale == 'ru-RU':
                if not any(part in relative_file.file.full_path for part in ["robust-toolbox", "corvax", "_horizon", "_Horizon"]):
                    self.warn_en_analog_not_exist(relative_file)
            else:
                raise Exception(f'Неизвестная локаль: {relative_file.locale}')

        return self.created_files

    def get_files_pars(self):
        en_files = self.project.get_fluent_files_by_dir(project.en_locale_dir_path)
        ru_files = self.project.get_fluent_files_by_dir(project.ru_locale_dir_path)

        en_rel = [self.get_relative_path_dict(f, 'en-US') for f in en_files]
        ru_rel = [self.get_relative_path_dict(f, 'ru-RU') for f in ru_files]

        return py_.group_by(en_rel + ru_rel, 'relative_path_from_locale')

    def create_ru_analog(self, en_relative_file: RelativeFile) -> FluentFile:
        ru_path = en_relative_file.file.full_path.replace('en-US', 'ru-RU')
        ru_file = FluentFile(ru_path)
        ru_file.save_data(en_relative_file.file.read_data())
        logging.info(f'Создан файл {ru_path}')
        return ru_file

    def warn_en_analog_not_exist(self, ru_relative_file: RelativeFile):
        file: FluentFile = ru_relative_file.file
        en_path = file.full_path.replace('ru-RU', 'en-US')
        logging.warning(f'Нет английского аналога: {file.full_path} → {en_path}')


class KeyFinder:
    def __init__(self, files_dict):
        self.files_dict = files_dict
        self.changed_files: typing.List[FluentFile] = []

    def execute(self) -> typing.List[FluentFile]:
        self.changed_files = []
        for pair in self.files_dict:
            ru_file_rel = py_.find(self.files_dict[pair], {'locale': 'ru-RU'})
            en_file_rel = py_.find(self.files_dict[pair], {'locale': 'en-US'})

            if not ru_file_rel or not en_file_rel:
                continue

            self.compare_files(en_file_rel.file, ru_file_rel.file)

        return self.changed_files

    def compare_files(self, en_file, ru_file):
        ru_parsed: ast.Resource = ru_file.parse_data(ru_file.read_data())
        en_parsed: ast.Resource = en_file.parse_data(en_file.read_data())

        self.write_to_ru_files(ru_file, ru_parsed, en_parsed)
        self.log_not_exist_en_files(en_file, ru_parsed, en_parsed)

    def write_to_ru_files(self, ru_file, ru_parsed, en_parsed):
        for idx, en_msg in enumerate(en_parsed.body):
            if not isinstance(en_msg, ast.Message):
                continue

            ru_idx = py_.find_index(ru_parsed.body, lambda msg: isinstance(msg, ast.Message) and self.find_duplicate_message_id_name(msg, en_msg))
            have_changes = False

            # Add new keys
            if ru_idx == -1:
                ru_parsed.body.insert(min(idx, len(ru_parsed.body)), en_msg)
                have_changes = True
            else:
                ru_entry = ru_parsed.body[ru_idx]
                if isinstance(ru_entry, ast.Junk):
                    continue

                if getattr(en_msg, 'attributes', None):
                    if not getattr(ru_entry, 'attributes', None):
                        ru_entry.attributes = en_msg.attributes
                        have_changes = True
                    else:
                        for en_attr in en_msg.attributes:
                            if not py_.find(ru_entry.attributes, lambda ru_attr: ru_attr.id.name == en_attr.id.name):
                                ru_entry.attributes.append(en_attr)
                                have_changes = True

            if have_changes:
                serialized = serializer.serialize(ru_parsed)
                self.save_and_log_file(ru_file, serialized, en_msg)

    def log_not_exist_en_files(self, en_file, ru_parsed, en_parsed):
        for ru_msg in ru_parsed.body:
            if not isinstance(ru_msg, ast.Message):
                continue
            if not py_.find(en_parsed.body, lambda en_msg: self.find_duplicate_message_id_name(ru_msg, en_msg)):
                logging.warning(f'Ключ "{FluentAstAbstract.get_id_name(ru_msg)}" не найден в {en_file.full_path}')

    def save_and_log_file(self, file, data, msg):
        file.save_data(data)
        logging.info(f'Добавлен ключ: {FluentAstAbstract.get_id_name(msg)} → {file.full_path}')
        self.changed_files.append(file)

    def find_duplicate_message_id_name(self, a, b):
        return FluentAstAbstract.get_id_name(a) == FluentAstAbstract.get_id_name(b)


######################################## Var definitions ###############################################################

logging.basicConfig(level = logging.INFO)
project = Project()
parser = FluentParser()
serializer = FluentSerializer(with_junk=True)

files_finder = FilesFinder(project)
key_finder = KeyFinder(files_finder.get_files_pars())

########################################################################################################################

print('Проверка актуальности файлов ...')
created_files = files_finder.execute()
if len(created_files):
    print('Форматирование созданных файлов ...')
    FluentFormatter.format(created_files)
print('Проверка актуальности ключей ...')
changed_files = key_finder.execute()
if len(changed_files):
    print('Форматирование изменённых файлов ...')
    FluentFormatter.format(changed_files)
