# Wheel of Wuck

Getting tired of picking trinkets and bindings on your own? Need to spice it up a little? Well, not to worry - the mighty **Wheel of Wuck** is here to save you! Gamble with your run's trinkets and bindings by scrolling through the wheel of names that define your run. Six wheels, one after the other: difficulty, route, how many bindings, which bindings, a trinket and a 1 in 1000 chance for the Chimney.

Everything the mod sets the new game screen can set too, so all wheel runs still count for the leaderboards.

## Installing

With r2modman, simply search for `WheelOfWuck` and install the mod.

Got the zip instead? Go to `Settings > Profile > Import local mod`.

Manual install: install [BepInEx 5](https://thunderstore.io/c/white-knuckle/p/BepInEx/BepInExPack/), start the game once, unzip the mod into **its own folder** inside `BepInEx/plugins`.

## How it works

- Go to Campaign, press Play and there will be a **Wheel** button next to Play on the new game screen (also on the screen after you die)
- Spin through the wheels. **Cancel** leaves everything as it was, **Last roll** takes the previous roll again, **Reset** starts over from the first wheel (and resets the last roll)
- **Settings** (before the first spin) lets you enable/disable what can land on each wheel. Locked stuff (like shortcuts) is greyed out
- When the last wheel is done, the mod selects all the options you rolled in the in-game menu and you're all good to go
- The route and the Hot Zone are yours to take, the mod only tells you which. Chimney means the run starts in the Chimney first (see below)

Things the mod can't set for you are left off the wheel - this also includes shortcuts and bindings you haven't unlocked yet.

## In the run

A small panel in the top right corner shows the roll. Press `F8` to enable/disable the wheel run HUD. Shortcuts turn green when you take them, and red when you take one the wheel didn't pick (off route). If you climb past the entrance of a shortcut the wheel picked, it will turn red too and 5 seconds later the run restarts automatically.
You can disable this via the `RestartOnSkip` config option.

Some rolls are more than one run! Ladder + Hot Zone means that you have to beat the game in the Ladder first (by either looping once and dying, or exiting through the vent after one loop), then start over and go through the Hot Zone.

Chimney: Chimney starts first with the rolled bindings and trinkets. After every loop a prompt asks whether to keep climbing (`F3`) or end the run there (`F6`), which is a normal death so the Chimney's run is properly submitted to the leaderboard. Restart on that screen then starts the campaign run, however dying before the first loop means the Chimney run was not successful.

Restarting the game puts the trinkets and bindings back the way they were before the wheel roll.

Every finished wheel run is written to `BepInEx/config/senkodev.whiteknuckle.wheelofwuck.history.json`.

## Settings

In the `BepInEx/config/senkodev.whiteknuckle.wheelofwuck.cfg` file and in the Mod Menu the following settings are available: `HudKey`, `ShowHud`, `HudPosition`, `HudScale`, `TickVolume`, `RestartOnSkip`, `TrinketPerBinding`, `ChimneyStayKey`, `ChimneyGoKey`.

`TrinketPerBinding` is on by default, and can be unticked in the wheel's settings. When it is on, the trinket wheel spins once per rolled binding + 1 free, so 3 bindings give 4 trinkets. Turn it off to always roll a single trinket.

## Building

You will need the .NET SDK and the game with [BepInEx](https://old.thunderstore.io/c/white-knuckle/p/BepInEx/BepInExPack/) **already installed**.

```
dotnet build src/WheelOfWuck.csproj -c Release
```

This also copies the mod into your r2modman profile. If your game and/or profile are somewhere else, copy `LocalPaths.props.example` to `LocalPaths.props` and put your paths in there. Add `-p:Deploy=false` if you only want to build the mod.

The wheels are in the `src/wheels.json` file. It is built into the dll, so rebuild after changing it.

`tools/package.ps1` makes the zip package for the Thunderstore listing.

## Dedicated to

This mod is dedicated to **Sckurge**, who you can find on [Twitch](https://www.twitch.tv/sckurge) and [YouTube](https://www.youtube.com/@SckurgeWK). He is also the author of the original wheel which was used for this mod! :D

## Credits

- **Dark Machine Games** for White Knuckle
- The [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX) teams for the mod loader and patching
- **sinai-dev** and **yukiaiji** for [UnityExplorer](https://github.com/yukieiji/UnityExplorer)
- The idea and the wheel options for the mod were taken from the **Wheel of Wuck**, which was made by **Sckurge**
- Code, icons, design and testing are by [senkodev](https://senko.dev)

If you can design a better icon for the mod, please contact me through Discord (senkodev) or [Telegram](https://t.me/senkodev) 🥹
