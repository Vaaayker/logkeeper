"""16-байтовий запис журналу. Точні поля й типи — у FORMAT_SPEC_P3.md."""
from dataclasses import dataclass

from logrepl.errors import FormatError  # noqa: F401  (знадобиться в parse)


@dataclass(frozen=True)
class Record:
    timestamp_ms: int
    source_id: int
    event_code: int
    metric_value: int  # тип уточнити за FORMAT_SPEC_P3.md
    category: int
    severity: int
    critical: int
    record_flags: int


def serialize(record: Record) -> bytes:
    """Record -> рівно 16 байтів."""
    raise NotImplementedError


def parse(data: bytes) -> Record:
    """16 байтів -> Record. Некоректні дані -> FormatError."""
    raise NotImplementedError
