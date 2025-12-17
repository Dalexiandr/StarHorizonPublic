using Robust.Shared.Utility;

namespace Content.Shared._Horizon.Sponsors.Systems
{
    public sealed class SpriteOverrideSystem : EntitySystem
    {
        // Словарь, где ключ - это ник игрока, а значение - спецификатор спрайта.
        private readonly Dictionary<string, SpriteSpecifier> _playerSprites = new()
        {
            // Пример записи для теста с использованием .rsi файла
            {"Joulerk", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/Joulerk.rsi"), "state_name")},
            {"localhost@EvilBug", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/evilneko.rsi"), "state_name")},
            {"EvilBug", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/evilneko.rsi"), "state_name")},
            {"Lemird", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/evilneko.rsi"), "state_name")},
            {"DenCash", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/headadmin.rsi"), "state_name")},
            {"EXPERRIENCEE", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/joker.rsi"), "state_name")},
            {"CaxapoK", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/runtime.rsi"), "state_name")},
            {"Iris_ka", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/kitsune.rsi"), "state_name")},
            {"WiNNER", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/hellsing.rsi"), "state_name")},
            {"Rimix", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/angel.rsi"), "state_name")},
            {"Xigovir", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/uas.rsi"), "state_name")},
            {"TheGypsyBaron", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/baron.rsi"), "state_name")},
            {"KoTiKo43", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/kotiko.rsi"), "state_name")},
            {"por1k", new SpriteSpecifier.Rsi(new ResPath("/Textures/_Horizon/Sponsors/Ghosts/harpy.rsi"), "state_name")},
        };

        /// <summary>
        /// Получаем спецификатор спрайта для игрока по его нику.
        /// </summary>
        /// <param name="playerName">Ник игрока</param>
        /// <returns>Спецификатор спрайта</returns>
        public SpriteSpecifier? GetSpriteForPlayer(string playerName)
        {
            if (_playerSprites.TryGetValue(playerName, out var spriteSpecifier))
            {
                return spriteSpecifier;
            }

            return null;
        }
    }
}
