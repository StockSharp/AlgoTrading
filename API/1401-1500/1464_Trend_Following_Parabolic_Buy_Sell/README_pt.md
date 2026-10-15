# Estratégia de Seguimento de Tendência Parabolic Compra Venda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina Parabolic SAR com cruzamentos de médias móveis.
Entradas compradas ocorrem quando o preço está acima de uma SMA de tendência longa, a EMA rápida cruza acima da EMA lenta e o SAR é de alta.
Entradas vendidas usam as condições opostas.
O stop loss é colocado na SMA de tendência e o take profit usa uma razão risco/recompensa.

## Detalhes

- **Entrada**:
  - **Comprado**: preço > SMA de tendência, EMA rápida cruza acima da EMA lenta, SAR de alta
  - **Vendido**: preço < SMA de tendência, EMA rápida cruza abaixo da EMA lenta, SAR de baixa
- **Saída**:
  - stop na SMA de tendência
  - take profit = risco/recompensa * distância da entrada até a SMA de tendência
- **Indicadores**: Parabolic SAR, SMA, EMA
- **Período**: configurável
- **Tipo**: Seguidor de tendência
