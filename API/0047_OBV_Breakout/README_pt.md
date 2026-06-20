# OBV Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
On-Balance Volume (OBV) rastreia a pressão compradora e vendedora acumulando volume. Esta estratégia busca o OBV romper acima de uma máxima ou abaixo de uma mínima na janela de observação enquanto o preço confirma o movimento.

Os testes indicam um retorno anual médio de aproximadamente 178%. Funciona melhor no mercado de ações.

Um rompimento no OBV sugere forte interesse. O sistema vai comprado se o OBV supera seu máximo anterior, ou vendido se rompe a mínima. O cruzamento do OBV com sua média móvel sinaliza uma saída.

Isso combina momentum de volume com ação do preço.

## Detalhes

- **Critérios de entrada**: OBV supera o valor mais alto ou mais baixo no período de observação.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: OBV cruza sua MA ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `OBVMAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: OBV, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

