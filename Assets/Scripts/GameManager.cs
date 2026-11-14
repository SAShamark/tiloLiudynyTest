using System;
using Gameplay;
using Gameplay.Entities.Player;
using UnityEngine;

[Serializable]
public class GameManager
{
    [SerializeField] private PlayerControl _player;
    [SerializeField] private GameplayManager _gameplayManager;

    public void Initialize()
    {
        _player.Initialize();
        _gameplayManager.Initialize(_player);
    }

    public void Dispose()
    {
        _gameplayManager.Dispose();
        _player.Dispose();
    }
}
