# Estratégia Sniper de Armadilha de Reversão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Reversal Trap Sniper procura armadilhas de RSI onde o momentum se reinicia mas o preço continua se movendo.
Compra após uma reversão de sobrecompra que ainda fecha mais alto, e vende após uma reversão de sobrevenda que ainda fecha mais baixo.

## Detalhes

- **Critérios de entrada**: RSI em sobrecompra/sobrevenda há três barras com o RSI atual cruzando de volta e o preço continuando na mesma direção
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop ATR, alvo ou máximo de barras
- **Stops**: Baseado em ATR
- **Valores padrão**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `RiskReward` = 2
  - `MaxBars` = 30
  - `AtrLength` = 14
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: RSI, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
