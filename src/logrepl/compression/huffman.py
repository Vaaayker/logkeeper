"""Канонічний Гаффман. Правила формату — FORMAT_SPEC_P3.md."""


def code_lengths(data: bytes) -> dict[int, int]:
    """Довжини кодів Гаффмана: {байт: довжина коду}."""
    raise NotImplementedError


def canonical_codes(lengths: dict[int, int]) -> dict[int, str]:
    """Канонічні коди тільки з довжин."""
    raise NotImplementedError


def encode(data: bytes) -> bytes:
    raise NotImplementedError


def decode(blob: bytes) -> bytes:
    raise NotImplementedError
