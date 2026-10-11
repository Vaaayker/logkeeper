"""Завантаження train.csv / valid.csv."""


def load_events(path: str) -> list[dict]:
    """Читає CSV, повертає список рядків (назви колонок — як у файлі)."""
    raise NotImplementedError


def class_counts(events: list[dict]) -> dict[int, int]:
    """Скільки записів кожного класу (0/1/2)."""
    raise NotImplementedError
