using System;
using Gameplay;
using Gameplay.Entities.Player;
using UI.Settings;
using UnityEngine;

[Serializable]
public class GameManager
{
    [SerializeField] private PlayerControl _player;
    [SerializeField] private SettingsMenu _settings;
    [SerializeField] private GameplayManager _gameplayManager;

    public void Initialize()
    {
        _player.Initialize();
        _gameplayManager.Initialize(_player);
        _settings.Initialize(_player, _gameplayManager.Participant);
    }

    public void Dispose()
    {
        _gameplayManager.Dispose();
    }
}
