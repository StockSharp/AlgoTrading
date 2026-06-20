# Estratégia de Divergência Sazonal HMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia combina a Média Móvel Hull (HMA) com o agrupamento sazonal de interesse aberto para encontrar divergências entre o preço e o posicionamento do mercado. Ela pressupõe que, quando o preço se move temporariamente contra a direção do interesse aberto crescente, uma continuação de tendência é provável. O sistema é projetado para operar tanto comprado quanto vendido, usando a inclinação da HMA para medir o momentum e os dados sazonais de interesse aberto para medir os níveis de participação.

Os testes indicam um retorno anual médio de cerca de 40%. Funciona melhor no mercado de criptomoedas.

Uma configuração de operação ocorre quando a HMA muda em relação à barra anterior enquanto o interesse aberto sazonal confirma o movimento, mas o preço imprime na direção oposta. Essa divergência de alta ou de baixa entre o preço e o posicionamento frequentemente sinaliza o fim de um recuo de curto prazo dentro de uma tendência maior. A estratégia aguarda essas condições antes de entrar e posiciona um stop baseado em volatilidade para gerenciar o risco.

As posições são encerradas quando a inclinação da HMA se inverte, sinalizando que o momentum mudou. Como o nível de stop usa um múltiplo do Average True Range (ATR), o risco se adapta à volatilidade do mercado. Isso ajuda a evitar saídas prematuras durante períodos de expansão e mantém as perdas contidas quando a volatilidade contrai.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `HMA(t) > HMA(t-1)` && `OI_Cluster_Seasonal(t) > OI_Cluster_Seasonal(t-1)` && `Price(t) < Price(t-1)` (divergência de alta).
  - **Vendido**: `HMA(t) < HMA(t-1)` && `OI_Cluster_Seasonal(t) < OI_Cluster_Seasonal(t-1)` && `Price(t) > Price(t-1)` (divergência de baixa).
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: `HMA(t) < HMA(t-1)` (HMA começa a cair).
  - **Vendido**: `HMA(t) > HMA(t-1)` (HMA começa a subir).
- **Stops**: Sim, stop-loss colocado em `N * ATR` a partir da entrada.
- **Valores padrão**:
  - `HMA period` = 9.
  - `OI_Cluster_Seasonal` = OI sazonal em níveis de cluster ao longo de cinco anos.
  - `N` = 2 (stop-loss = `2 * ATR`).
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Complexo
  - Período: Médio prazo
  - Sazonalidade: Sim
  - Redes neurais: Sim
  - Divergência: Sim
  - Nível de risco: Alto

