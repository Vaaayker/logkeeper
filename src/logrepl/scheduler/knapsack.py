"""Динамічне програмування: рюкзак 0/1, далі багатоваріантний (0/1/2/3 копії)."""


def knapsack_01(weights: list[int], values: list[int], capacity: int) -> tuple[int, list[int]]:
    """Повертає (найбільша цінність, індекси вибраних предметів)."""
    raise NotImplementedError


def choose_copies(weights: list[int], utilities: list[list[int]], budget: int) -> list[int]:
    """Багатоваріантний рюкзак: r_i для кожної події."""
    raise NotImplementedError
