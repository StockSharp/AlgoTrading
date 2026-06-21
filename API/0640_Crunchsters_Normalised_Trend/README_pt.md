# Estratégia de Tendência Normalizada Crunchsters
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que normaliza os retornos e aplica uma Hull Moving Average ao preço normalizado cumulativo.
Entra comprado quando o preço normalizado cruza acima da HMA e vendido quando cruza abaixo.

Os testes indicam um retorno anual médio de aproximadamente 105%. Tem melhor desempenho no mercado cripto.

Os retornos normalizados permitem escalar o preço pela volatilidade recente. Um stop baseado em ATR gerencia o risco.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `nPrice` cruza acima de `HMA`
  - Vendido: `nPrice` cruza abaixo de `HMA`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento oposto ou stop por ATR
- **Stops**: Baseado em ATR usando `StopMultiple`
- **Valores padrão**:
  - `NormPeriod` = 14
  - `HmaPeriod` = 100
  - `HmaOffset` = 0
  - `StopMultiple` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Hull Moving Average, Standard Deviation, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
