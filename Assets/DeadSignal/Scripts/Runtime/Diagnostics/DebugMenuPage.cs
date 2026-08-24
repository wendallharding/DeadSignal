using System;
using UnityEngine;
using DeadSignal.Application;

namespace DeadSignal.Diagnostics
{
    public abstract class DebugMenuPage : MonoBehaviour
    {
        protected DeadSignalGame Game { get; private set; }
        protected DeadSignalDebugMenu Menu { get; private set; }

        public void Configure(DeadSignalDebugMenu menu, DeadSignalGame game)
        {
            Menu = menu;
            Game = game;
            OnConfigured();
        }

        protected virtual void OnConfigured()
        {
        }

        protected void Run(Action action, string confirmation) => Menu.Execute(action, confirmation);
    }
}
