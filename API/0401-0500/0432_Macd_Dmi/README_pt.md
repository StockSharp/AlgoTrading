# Estratégia MACD + DMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina o Moving Average Convergence Divergence com o Directional Movement Index para operar apenas quando a força da tendência é confirmada. O sistema aguarda um cruzamento do MACD e verifica que a linha direcional dominante supera a linha oposta enquanto o ADX está acima de um nível-chave.

A estratégia é projetada para posições compradas e vendidas. Ao combinar filtros de momentum e tendência, visa evitar sinais falsos em mercados laterais. Stops de proteção baseados em volatilidade mantêm o risco contido.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A linha MACD cruza acima da sinal, +DI > -DI, e ADX acima do nível-chave.
  - **Vendido**: A linha MACD cruza abaixo da sinal, -DI > +DI, e ADX acima do nível-chave.
- **Critérios de saída**:
  - Sinal inverso ou stop de volatilidade atingido.
- **Indicadores**:
  - MACD (rápida 12, lenta 26, sinal 9)
  - Directional Movement Index (comprimento 14, suavização ADX 14)
- **Stops**: Usa stop-loss e take-profit integrados via StartProtection.
- **Valores padrão**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **Filtros**:
  - Seguidor de tendência
  - Funciona em múltiplos períodos
  - Indicadores: MACD, DMI
  - Stops: Sim
  - Complexidade: Moderado
