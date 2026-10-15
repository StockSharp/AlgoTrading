# Estratégia de Robô Autônomo de 5 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Robô Autônomo de 5 Minutos opera em um período de 5 minutos.
Vai comprado quando o preço está em tendência de alta e a pressão compradora supera a vendedora,
e vai vendido nas condições opostas.

## Detalhes

- **Critérios de entrada**: Tendência de alta (fechamento acima da SMA de 50 períodos e acima do fechamento de 6 barras atrás) com volume comprador maior que vendedor.
- **Critérios de saída**: Reversão de posição no sinal oposto.
- **Stops**: Stop-loss de 3% e take profit de 29% a partir do preço de entrada.
- **Valores padrão**:
  - `MaLength` = 50
  - `VolumeLength` = 10
  - `StopLossPercent` = 3
  - `TakeProfitPercent` = 29
  - `CandleType` = 5m
