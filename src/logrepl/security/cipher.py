"""AES-256-CTR, лічильник = nonce(12) || uint32_BE(i), i починається з 1."""


def aes_ctr(key: bytes, nonce: bytes, data: bytes) -> bytes:
    raise NotImplementedError
