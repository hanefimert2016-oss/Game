#!/usr/bin/env python3
"""Create a QR code image for the game URL or any text."""

from __future__ import annotations

import argparse
from pathlib import Path

import qrcode


def build_qr(data: str, output: Path) -> None:
    qr = qrcode.QRCode(
        version=None,
        error_correction=qrcode.constants.ERROR_CORRECT_Q,
        box_size=12,
        border=3,
    )
    qr.add_data(data)
    qr.make(fit=True)
    image = qr.make_image(fill_color="#0b1220", back_color="#f8fbff")
    image.save(output)


def main() -> None:
    parser = argparse.ArgumentParser(description="Mine Rails için QR kod oluşturur.")
    parser.add_argument("data", help="QR içine yazılacak URL veya metin")
    parser.add_argument("output", nargs="?", default="mine-rails-qr.png", help="Çıktı dosyası")
    args = parser.parse_args()
    output = Path(args.output).expanduser().resolve()
    build_qr(args.data, output)
    print(f"QR kod oluşturuldu: {output}")


if __name__ == "__main__":
    main()
