# EnhancedCartel

Allows cartel to order other products than default ones.

![icon](https://raw.githubusercontent.com/k073l/s1-enhancedcartel/master/assets/icon.png)

**Warning**: >0.4.0f5 only

## Installation
1. Install MelonLoader
2. Extract the zip file
3. Place the dll file into the Mods directory for your branch
    - For none/beta use IL2CPP
    - For alternate/alternate beta use Mono
4. Launch the game
5. Preferences file will appear once you quit the game

## Configuration
1. Open the config file in `UserData/MelonLoader.cfg`
2. Edit the config file
```ini
[EnhancedCartel]
# Minimum quantity of products in cartel requests
ProductQuantityMin = 10
# Maximum quantity of products in cartel requests
ProductQuantityMax = 40
# Use products that are listed for sale in cartel requests
UseListedProducts = true
# Use products that have been discovered in cartel requests. Overrides UseListedProducts if true
UseDiscoveredProducts = false
# Round requested product quantities to nearest multiple of this value (1 = no rounding) [1-20]
RoundingMultiple = 5
```

## Credits
- [TVGS](https://www.scheduleonegame.com/) - Benzies logo
- [Flaticon](https://www.flaticon.com/free-icon/mixer_3690065) - Mixer icon
