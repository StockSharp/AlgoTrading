# Caixa eletrônico 5 min legado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Cash Machine 5 min Legacy é uma versão StockSharp do MetaTrader 4 consultor especialista `CashMachine_5min`. O sistema reage às reversões de impulso detectadas pelo oscilador DeMarker e pelo oscilador rápido Stochastic em velas de cinco minutos. Uma vez aberta uma posição, a estratégia esconde os seus níveis protetores de stop-loss e take-profit, revelando-os apenas à lógica interna, para que as paragens do lado da corretora não sejam visíveis. O lucro é protegido de forma incremental em três marcos definidos pelo usuário.

## Lógica estratégica
### Condições de entrada
* **Configuração longa** – espere que o valor do DeMarker ultrapasse o limite de 0,30 enquanto a linha Stochastic %K cruza simultaneamente acima de 20. Ambas as condições devem mudar de estado da vela finalizada anterior para a atual. Quando estável, a estratégia compra no mercado usando o volume de pedidos configurado.
* **Configuração curta** – espelho do caso longo: DeMarker deve passar de 0,70 e Stochastic %K deve cruzar abaixo de 80. O sinal é válido somente quando a vela anterior estava no lado oposto de ambos os limites. A estratégia vende a descoberto por mercado quando nenhuma posição está aberta.

### Gestão comercial
* **Limites de risco ocultos** – uma posição longa fecha se o preço cair na distância de `Hidden Stop Loss` ou subir na distância de `Hidden Take Profit`. Shorts usam as condições simétricas com os limites invertidos. Os níveis são monitorados internamente sem a colocação de ordens reais de stop.
* **Trailing stop faseado** – três pontos de verificação de lucro (`Target TP1`, `Target TP2`, `Target TP3`) apertam o stop à medida que o preço avança. Para posições compradas, quando o preço atinge um ponto de verificação, o stop é elevado até a máxima da vela menos `(target − 13)` pips. Para vendas, o stop é reduzido para o mínimo da vela mais `(target + 13)` pips. Cada estágio é aplicado apenas uma vez e nunca afrouxado.
* **Trailing execução** – após pelo menos um estágio ser armado, tocar no trailing stop fecha a posição por ordem de mercado.

### Mecânica de apoio
* A estratégia estima automaticamente o tamanho do pip a partir da etapa de preço do título, suportando símbolos forex de 4/2 dígitos e 5/3 dígitos.
* Os cálculos e sinais dos indicadores são conduzidos pelo tipo de vela selecionável (velas de cinco minutos por padrão). Apenas velas prontas são processadas.

## Parâmetros
* **Hidden Take Profit** – distância de take-profit oculta em pips (padrão: `60`).
* **Hidden Stop Loss** – distância de stop-loss oculta em pips (padrão: `30`).
* **Target TP1 / TP2 / TP3** – marcos de lucro em pips que armam o trailing stop escalonado (padrão: `20`, `35`, `50`).
* **Volume de ordens** – volume de ordens de mercado usado para entradas (padrão: `0.2`).
* **DeMarker Length** – período médio para o oscilador DeMarker (padrão: `14`).
* **Stochastic Comprimento** – lookback base para o oscilador Stochastic (padrão: `5`).
* **Stochastic %K** – fator de suavização para a linha %K (padrão: `3`).
* **Stochastic %D** – fator de suavização para a linha %D (padrão: `3`).
* **Tipo de vela** – período usado para calcular os indicadores (padrão: velas de cinco minutos).

## Notas adicionais
* A estratégia abre apenas uma posição por vez e não reverterá imediatamente; ele espera que a negociação atual feche antes que um novo sinal seja acionado.
* Os níveis de proteção são aplicados no código por meio de saídas de mercado, portanto, não há ordens stop pendentes na carteira de pedidos.
* O pacote contém apenas a implementação do C#; nenhuma versão do Python é fornecida.
