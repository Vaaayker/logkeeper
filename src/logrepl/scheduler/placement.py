"""Куди й у якому порядку писати копії (не більше одного batch на вузол у вікні)."""


def plan_batches(copies: list[int], nodes: list[dict]) -> dict[str, list[int]]:
    raise NotImplementedError
