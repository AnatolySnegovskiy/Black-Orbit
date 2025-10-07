namespace Black_Orbit.Scripts.WeaponSystem.Base
{
    /// <summary>
    /// Необязательный интерфейс для оружия, предоставляющий информацию о боезапасе.
    /// Если оружие его реализует, AI сможет принимать решения о перезарядке осознанно.
    /// </summary>
    public interface IWeaponAmmoInfo
    {
        int CurrentAmmo { get; }
        int MagazineSize { get; }
    }
}
