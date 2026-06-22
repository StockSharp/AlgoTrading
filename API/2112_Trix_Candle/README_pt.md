# Estratégia Trix Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera reversões com base no indicador Trix Candle, que aplica uma tripla média móvel exponencial aos preços de abertura e fechamento das velas e colore cada vela dependendo se o fechamento suavizado está acima ou abaixo da abertura suavizada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: vela anterior de alta (cor 2) e cor da vela atual < 2
  - **Vendido**: vela anterior de baixa (cor 0) e cor da vela atual > 0
- **Comprado/Vendido**: Comprado e Vendido
- **Critérios de saída**:
  - Comprado: vela anterior de baixa (cor 0)
  - Vendido: vela anterior de alta (cor 2)
- **Stops**: Não
- **Valores padrão**:
  - `TRIX Period` = 14
  - `Candle Type` = 4h
  - `Allow Buy Open` = true
  - `Allow Sell Open` = true
  - `Allow Buy Close` = true
  - `Allow Sell Close` = true
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Triple Exponential Moving Average
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
