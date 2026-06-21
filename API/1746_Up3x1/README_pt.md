# Estratégia Up3x1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Up3x1 usa três médias móveis simples para capturar mudanças de tendência:

- **SMA rápida**: reage rapidamente às mudanças de preço.
- **SMA média**: fornece confirmação adicional da tendência.
- **SMA lenta**: define a direção global do mercado.

### Regras de entrada

- **Compra** quando a SMA rápida cruza acima da SMA média e ambas estão abaixo da SMA lenta.
- **Venda** quando a SMA rápida cruza abaixo da SMA média e ambas estão acima da SMA lenta.

### Regras de saída

- Um take profit e stop loss fixos são aplicados a cada posição.
- Um trailing stop opcional pode proteger os lucros seguindo o preço após a entrada.

### Parâmetros

- `Volume` – tamanho da ordem.
- `TakeProfit` – alvo de lucro em unidades de preço.
- `StopLoss` – limite de perda em unidades de preço.
- `TrailingStop` – distância de trailing; definir como 0 para desabilitar.
- `FastPeriod`, `MiddlePeriod`, `SlowPeriod` – comprimentos das médias móveis.
- `CandleType` – período de candles usado para os cálculos.

A estratégia foi projetada para demonstração e pode ser personalizada para instrumentos ou condições de negociação específicos.
