# Sunless Skies
## User Interface
### `OfficerHudController`
* Manages bottom right display of officers and character avatar
* Add at the end of `DisplayHud`
```C#
private bool officerHudHidden = false;

this._disposables.Add((from x in this._playerInput.WhenKeyDown
where x.Option == ControlOption.Horn
select x).Subscribe(delegate(ControlVelocity y)
{
    if (officerHudHidden) {
        _presenter.SetActive(true);
    } else {
        _presenter.SetActive(false);
    }
    officerHudHidden = !officerHudHidden;
}));
```

### `FooterController`
* Manages right display of utility and option buttons
* Hide by calling `HideFooterObject` at the end of `DisplayFooter`

### `SurvivalHUDController`
* Manages bottom left display of resources such as fuel, terror, and hull
* Hide by calling `HideHUDUiObject` at the end of `DisplayHUD`

### `PlayerController`
* Spawns the player
* Hide the locomotive by calling `HideShip` at the end of it
* Call this method in `Start`
```C#
public bool speedup = false;

public void ExtraPlayerHotkeys() {
    this._disposables.Add((from x in this._playerInput.WhenKeyDown where x.Option == ControlOption.Cruise
    select x).Subscribe(delegate(ControlVelocity y)
    {
        float multiplier = 2f;
        if (speedup) {
            this.MaxSpeed *= 2;
            this.ForwardThrust *= 2;
            this.BackwardThrust *= 2;
            this.LinearDrag *= 2;
        } else {
            this.MaxSpeed *= 2;
            this.ForwardThrust *= 2;
            this.BackwardThrust *= 2;
            this.LinearDrag *= 2;
        }
    }));
    this._disposables.Add((from x in this._playerInput.WhenKeyDown
    where x.Option == ControlOption.Horn
    select x).Subscribe(delegate(ControlVelocity y)
    {
        if (PrefabContainer.gameObject.active) {
            PrefabContainer.gameObject.active = false;
            AngularThrust = 1f;
            _characterService.CurrentCharacter.AcquireQualityAtExplicitLevel(_wellKnownQualityProvider.Hull(), _characterService.CurrentCharacter.GetEffectiveQualityLevel(_wellKnownQualityProvider.MaxHull()));
            _characterService.CurrentCharacter.AcquireQualityAtExplicitLevel(_wellKnownQualityProvider.Crew(), _characterService.CurrentCharacter.GetEffectiveQualityLevel(_wellKnownQualityProvider.Quarters()));
            _characterService.CurrentCharacter.AcquireQualityAtExplicitLevel(_wellKnownQualityProvider.Fuel(), Math.Max(5, _characterService.CurrentCharacter.GetEffectiveQualityLevel(_wellKnownQualityProvider.Fuel())));
            _characterService.CurrentCharacter.AcquireQualityAtExplicitLevel(_wellKnownQualityProvider.Supplies(), Math.Max(5, _characterService.CurrentCharacter.GetEffectiveQualityLevel(_wellKnownQualityProvider.Supplies())));
            _characterService.CurrentCharacter.AcquireQualityAtExplicitLevel(_wellKnownQualityProvider.Terror(), 0);
            _hudController.HideHUDUiObject();
        } else {
            PrefabContainer.gameObject.active = true;
            AngularThrust = 20000f;
            _hudController.DisplayHUD();
        }
    }));
    this._disposables.Add((from x in this._playerInput.WhenKeyDown
    where x.Option == ControlOption.LightToggle
    select x).Subscribe(delegate(ControlVelocity y)
    {
        GetComponent<Rigidbody2D>().isKinematic = !GetComponent<Rigidbody2D>().isKinematic;
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }));
}
```

### `ChartController`
* Add at the end of `BindHotkeys`
```C#
private bool footerHidden = false;

this._disposables.Add((from x in this._playerInput.WhenKeyDown
where x.Option == ControlOption.Horn
select x).Subscribe(delegate(ControlVelocity y)
{
    if (footerHidden) {
        _footerController.DisplayFooter();
    } else {
        _footerController.HideFooterObject();
    }
    footerHidden = !footerHidden;
}));
```

### BepInEx
* Now using this to mod Unity games
* !WARNING! `UnityEngine.dll` in Sunless Skies is compiled against NET 4.0 instead of NET 3.5, so find another `UnityEngine.dll` with the correct runtimeversion