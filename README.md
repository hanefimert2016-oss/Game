# Mine Rails

Web tabanlı, kısa ve bağımlılıksız bir maden arama oyunu.

Oyunda üç keşif modu bulunur:

- **Yaya:** Dengeli, güvenli, düşük yakıt tüketimi yok.
- **Araba:** Hızlı keşif, yakıt harcar.
- **Tren:** Büyük tarama bonusu, ray istasyonlarına bağlıdır.

## Çalıştırma

```bash
python3 -m http.server 8000
```

Sonra tarayıcıdan `http://localhost:8000` adresini açın.

## QR kod oluşturma

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
python qr_generator.py "https://example.com/game" mine-rails.png
```