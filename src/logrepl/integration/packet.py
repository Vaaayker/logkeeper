"""Формат репліки LOG3: відкритий заголовок + шифроване навантаження + HMAC.

Сигнатури нижче — чернетка, уточнити за FORMAT_SPEC_P3.md.
"""


def build_packet(payload: bytes, password: str) -> bytes:
    raise NotImplementedError


def read_packet(data: bytes, password: str) -> bytes:
    """Перевіряє magic, довжину, HMAC; при помилці — FormatError (без аварії)."""
    raise NotImplementedError
