# Estratégia MOC Delta MOO Entry v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia registra o volume de compra e venda durante a sessão da tarde e utiliza o delta MOC resultante para operar a abertura do dia seguinte.

Das 14:50 às 14:55 acumula máximas, mínimas e volume separado de compra/venda. Às 14:55 calcula o percentual do delta de compra menos venda em relação ao volume diário total. Às 8:30 do dia seguinte, uma operação comprada é aberta se o delta estiver acima do limiar e a abertura estiver acima das SMAs de 15 e 30 períodos. Uma operação vendida usa as condições opostas. As posições incluem take profit e stop loss baseados em ticks e são encerradas às 14:50.

## Detalhes

- **Critérios de entrada**: Às 8:30, percentual do delta acima do limiar e preço acima do SMA15 e SMA30 para comprado; delta abaixo do limiar negativo e preço abaixo das SMAs para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take profit ou stop loss; todas as posições encerradas às 14:50.
- **Stops**: Sim.
- **Valores padrão**:
  - `TpTicks` = 20
  - `SlTicks` = 10
  - `DeltaThreshold` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Volume, SMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
