"""HMAC-SHA256 над заголовком і шифротекстом."""


def compute_hmac(key: bytes, header: bytes, ciphertext: bytes) -> bytes:
    raise NotImplementedError


def verify_hmac(key: bytes, header: bytes, ciphertext: bytes, tag: bytes) -> bool:
    raise NotImplementedError
