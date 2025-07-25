# BskyRoyale

A simple [BepInEx](https://github.com/BepInEx/BepInEx/) plugin that makes [Sewer Rave Twitter Royale](https://slitherpunk.itch.io/sewer-rave-twitter-royale) use BlueSky's API instead of Twitter's, thus making it playable again.

It is a fairly simple wrapper. It only replaces the functions actually used by the game, and only returns the information actually used by the game. It also contains a custom JSON parser because the one included in Unity broke for some reason.

# Installation

1. Download and extract [Sewer Rave Twitter Royale](https://slitherpunk.itch.io/sewer-rave-twitter-royale) to some location.
2. Download and extract [BepInEx](https://github.com/BepInEx/BepInEx/releases/latest) to the same location.
3. Run the game once and close it. This should create some extra folders.
4. Download and extract [the plugin](https://github.com/som1lse/BskyRoyale/releases/latest) to `BepInEx/plugins`.
5. Run the game and enter your (or somebody else's) BlueSky handle. The `.bsky.social` suffix is optional, and will be added if the handle doesn't contain a period.

# Building

1. `git clone` the repository.
2. Download and extract [Sewer Rave Twitter Royale](https://slitherpunk.itch.io/sewer-rave-twitter-royale) and [BepInEx](https://github.com/BepInEx/BepInEx/releases/latest) to the `SewerRaveTwitterRoyale` subfolder.
3. Run the game once and close it. This should create some extra folders.
4. You should now be able to build the project using Visual Studio. It'll automatically copy the plugin to the `SewerRaveTwitterRoyale/BepInEx/plugins` folder when built.
