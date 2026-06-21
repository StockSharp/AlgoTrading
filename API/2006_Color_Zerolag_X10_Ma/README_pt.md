# Estratégia Color Zerolag X10 Ma
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port simplificado do exemplo do MetaTrader **Exp_ColorZerolagX10MA.mq5**. Usa uma média móvel exponencial de zero defasagem para detectar mudanças de inclinação. Quando a média móvel vira para cima após diminuir por dois períodos, a estratégia abre ou reverte para uma posição comprada. Por outro lado, quando a média móvel vira para baixo após aumentar, abre ou reverte para uma posição vendida.

A lógica imita a ideia original onde um conjunto combinado de dez médias móveis suavizadas produz uma única linha codificada por cores. Aqui substituímos esse indicador complexo pelo `ZeroLagExponentialMovingAverage` integrado do StockSharp para manter a implementação compacta e reutilizável. O sistema trabalha no período de candles selecionado e pode habilitar ou desabilitar ações individuais (abrir/fechar comprado/vendido) via parâmetros.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `ZLEMA[t-2] > ZLEMA[t-1]` e `ZLEMA[t] > ZLEMA[t-1]`.
  - **Vendido**: `ZLEMA[t-2] < ZLEMA[t-1]` e `ZLEMA[t] < ZLEMA[t-1]`.
- **Comprado/Vendido**: Ambas as direções suportadas.
- **Critérios de saída**:
  - Posições compradas são fechadas quando um sinal vendido aparece e `BuyPosClose` está habilitado.
  - Posições vendidas são fechadas quando um sinal comprado aparece e `SellPosClose` está habilitado.
- **Stops**: Nenhum por padrão; saídas dependem de sinais opostos.
- **Valores padrão**:
  - `Length` = 20.
  - `CandleType` = período de 4 horas.
  - Todos os indicadores de ação (`BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose`) habilitados.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Simples
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
