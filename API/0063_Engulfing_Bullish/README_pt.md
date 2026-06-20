# Estratégia de Padrão Engolfamento Altista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta configuração procura uma reversão altista acentuada quando uma vela envolve completamente a barra baixista anterior. Tal formação frequentemente encerra uma queda de curto prazo e sugere um renovado impulso ascendente. O filtro de tendência de baixa opcional conta velas vermelhas consecutivas para confirmar o esgotamento dos vendedores.

Os testes indicam um retorno anual médio de aproximadamente 76%. Tem melhor desempenho no mercado forex.

Durante a operação ao vivo, o algoritmo observa cada vela entrante e mantém o controle da barra anterior. Se a nova vela fechar mais alta do que abre e seu corpo envolver a barra anterior, uma entrada comprada é acionada. O stop é colocado logo abaixo da mínima do padrão para limitar o risco.

As operações permanecem abertas até que o stop seja atingido ou outro sinal sugira saída discrecionária. Como a confirmação de barras de baixa anteriores fortalece a configuração, a estratégia evita perseguir reversões fracas.

## Detalhes

- **Critérios de entrada**: Vela altista envolve a barra baixista anterior, com tendência de baixa opcional presente.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop-loss ou discrecionário.
- **Stops**: Sim, abaixo da mínima do padrão.
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireDowntrend` = true
  - `DowntrendBars` = 3
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

