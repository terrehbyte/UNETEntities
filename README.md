# UNETEntities

A rudimentary implementation of the [Source Multiplayer Networking][srcnet] model for entites.

Each player's transform and input information are sent to each client, which
maintains a circular buffer of state updates. These state updates are evaluated
to find two updates to resimulate and interpolate between in order to provide
a reasonably accurate preview of a networked player.

[srcnet]:https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking

## Repository Structure

```
README.md                   This file! :)
Assets/                     Special folder for assets assets.
    UNETEntities/           Assets relating to the UNET entities project.
ProjectSettings/            Special folder for Unity settings.
```

# License

MIT License