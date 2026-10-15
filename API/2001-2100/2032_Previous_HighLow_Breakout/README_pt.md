# Estratégia de Rompimento do Máximo/Mínimo Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento que monitora o máximo e mínimo da vela anterior no período escolhido. Uma posição comprada é aberta quando a nova vela fecha acima do máximo anterior, enquanto uma posição vendida é aberta quando o fechamento cai abaixo do mínimo anterior. Um stop trailing e um take profit fixo gerenciam o risco e asseguram os ganhos.

O método visa capturar movimentos direcionais fortes após consolidação. Os stops trailing mantêm o risco ajustado à medida que o preço se move na direção favorável.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > PreviousHigh`
  - Vendido: `Close < PreviousLow`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Stop loss ou take profit
- **Stops**: Absolutos com trailing usando `StopLoss` e `TakeProfit`
- **Valores padrão**:
  - `StopLoss` = 50m
  - `TakeProfit` = 1000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim (trailing)
  - Complexidade: Iniciante
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
