"""PBKDF2-HMAC-SHA256: 100000 ітерацій, сіль 16 байтів, dkLen=64."""


def derive_keys(password: str, salt: bytes) -> tuple[bytes, bytes]:
    """Повертає (K_enc, K_mac): master[0:32] і master[32:64]."""
    raise NotImplementedError
