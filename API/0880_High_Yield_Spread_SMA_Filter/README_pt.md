# Estratégia de Spread de Alto Rendimento com Filtro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no nível do spread de crédito de alto rendimento ou do índice VIX. Quando o spread selecionado ultrapassa um limiar e o preço está no lado adequado de uma média móvel simples, a estratégia abre uma posição na direção escolhida.

As posições são mantidas por um número fixo de velas antes de serem fechadas. A estratégia opera exclusivamente com velas diárias.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: spread > limiar e (preço > SMA se o filtro estiver habilitado)
  - **Vendido**: spread < limiar e (preço < SMA se o filtro estiver habilitado)
- **Comprado/Vendido**: um lado por vez (parâmetro)
- **Critérios de saída**: posição fechada após o período de manutenção
- **Stops**: Não
- **Valores padrão**:
  - `Basis` = HighYieldSpread
  - `Threshold` = 5
  - `IsLong` = true
  - `HoldingPeriod` = 5
  - `UseSmaFilter` = true
  - `SmaLength` = 50
  - `CandleType` = 1 day
- **Filtros**:
  - Categoria: Spread
  - Direção: Configurável
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
