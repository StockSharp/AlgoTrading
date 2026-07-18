# Estratégia Combo Momentum Keltner Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina a comparação de momentum com um oscilador estocástico baseado em Keltner.  
As posições são escalonadas dinamicamente com base no capital e protegidas por um stop loss fixo.

## Detalhes

- **Critérios de entrada**:  
  - Comprado: `Momentum > 0` e `KeltnerStoch < Threshold`  
  - Vendido: `Momentum < 0` e `KeltnerStoch > Threshold`
- **Comprado/Vendido**: Ambos  
- **Critérios de saída**:  
  - Comprado: `KeltnerStoch > Threshold`  
  - Vendido: `KeltnerStoch < Threshold`
- **Stops**: `SlPoints` fixo abaixo/acima da entrada  
- **Valores padrão**:  
  - `MomLength` = 7  
  - `KeltnerLength` = 9  
  - `KeltnerMultiplier` = 0.5  
  - `Threshold` = 99  
  - `AtrLength` = 20  
  - `SlPoints` = 1185  
  - `EnableScaling` = true  
  - `BaseContracts` = 1  
  - `InitialCapital` = 30000  
  - `EquityStep` = 150000  
  - `MaxContracts` = 15  
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:  
  - Categoria: Seguidor de tendência  
  - Direção: Ambos  
  - Indicadores: Momentum, EMA, ATR  
  - Stops: Sim  
  - Complexidade: Intermediário  
  - Período: Médio prazo  
  - Sazonalidade: Não  
  - Redes neurais: Não  
  - Divergência: Não  
  - Nível de risco: Médio
