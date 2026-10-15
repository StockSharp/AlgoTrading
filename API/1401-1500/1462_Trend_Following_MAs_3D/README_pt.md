# Estratégia de Seguimento de Tendência com MAs 3D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza duas médias móveis simples curtas para detectar a direção da tendência.
Uma posição comprada é aberta quando a média de 5 períodos está acima da média de 10 períodos.
Uma posição vendida é aberta quando o oposto ocorre.

## Detalhes

- **Entrada**:
  - **Comprado**: SMA(5) > SMA(10)
  - **Vendido**: SMA(5) < SMA(10)
- **Saída**: sinal inverso
- **Indicadores**: SMA
- **Período**: configurável
- **Tipo**: Seguidor de tendência
