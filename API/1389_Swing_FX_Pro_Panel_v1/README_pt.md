# Swing FX Pro Panel v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de demonstração usando cruzamento de EMA com estatísticas básicas de desempenho. A EMA rápida cruzando acima da EMA lenta abre uma posição comprada, enquanto um cruzamento abaixo abre uma posição vendida. Cada operação usa alvos fixos de lucro e perda.

## Detalhes

- **Indicadores**: EMA
- **Parâmetros**:
  - `Initial Capital` – tamanho inicial da conta para estatísticas.
  - `Risk Per Trade` – porcentagem de risco por operação (informativo).
  - `Analysis Period` – duração do período usado para análise.
  - `Fast Length` – período da EMA rápida.
  - `Slow Length` – período da EMA lenta.
  - `Profit Target` – lucro em unidades de preço.
  - `Stop Loss` – perda em unidades de preço.
