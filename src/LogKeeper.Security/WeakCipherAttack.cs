namespace LogKeeper.Security;

/// <summary>Модуль атаки: ГА відновлює ключ навмисно слабкого шифру (AES не атакуємо).</summary>
public static class WeakCipherAttack
{
    public static byte[] Recover(byte[] ciphertext, int seed) => throw new NotImplementedException();
}
