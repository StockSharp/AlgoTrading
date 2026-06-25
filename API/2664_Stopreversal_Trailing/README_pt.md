# Estratégia Stopreversal Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Stopreversal Trailing reproduz o expert MT5 `Exp_Stopreversal.mq5`. Ela usa o indicador personalizado Stopreversal para construir uma linha de trailing stop dinâmica ao redor do preço de vela selecionado. Quando o preço perfura esta linha de trailing para cima, a estratégia trata isso como uma reversão altista, opcionalmente fecha a exposição vendida e abre uma nova posição comprada. Uma perfuração para baixo produz a ação baixista simétrica. Os sinais podem ser atrasados por um número configurável de barras fechadas para corresponder ao comportamento do assessor especialista original.

## Detalhes

- **Lógica de entrada**: reage às setas do indicador Stopreversal produzidas quando o preço cruza o trailing stop adaptativo.
- **Comprado/Vendido**: ambas as direções são suportadas com interruptores independentes para habilitar entradas compradas ou vendidas.
- **Lógica de saída**: sinais Stopreversal opostos podem fechar posições existentes; níveis protetores de stop-loss e take-profit também estão disponíveis.
- **Stops**: stop-loss e take-profit estáticos em passos de preço mais as reversões impulsionadas pelo indicador.
- **Fonte de dados**: qualquer período; o padrão usa velas de 4 horas, espelhando a chamada multi-período do expert original.
- **Atraso de sinal**: o parâmetro `SignalBar` atrasa a execução de ordens pelo número especificado de barras completadas (padrão 1 barra).
- **Gestão de risco**: stops duros opcionais expressos em passos de preço do instrumento; o serviço de proteção de posição é ativado no início.
- **Parâmetros do indicador**: o offset de trailing `Npips` controla a distância entre o preço e o stop; `PriceMode` seleciona o preço de vela usado pelo trailing stop.
- **Valores padrão**:
  - `Volume` = 1
  - `StopLossSteps` = 1000
  - `TakeProfitSteps` = 2000
  - `BuyPositionOpen` = true
  - `SellPositionOpen` = true
  - `BuyPositionClose` = true
  - `SellPositionClose` = true
  - `Npips` = 0.004
  - `PriceMode` = Close
  - `SignalBar` = 1

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Subscrição de velas usada tanto para cálculos de Stopreversal quanto para trading. O padrão é um período de 4 horas. |
| `Volume` | Tamanho base da ordem enviada ao entrar em uma nova posição. |
| `StopLossSteps` | Distância da entrada ao stop-loss em passos de preço; definir como 0 para desativar. |
| `TakeProfitSteps` | Distância da entrada ao take-profit em passos de preço; definir como 0 para desativar. |
| `BuyPositionOpen` | Habilita a abertura de posições compradas quando ocorre um sinal altista. |
| `SellPositionOpen` | Habilita a abertura de posições vendidas quando ocorre um sinal baixista. |
| `BuyPositionClose` | Fecha quaisquer posições compradas existentes quando um sinal baixista é recebido. |
| `SellPositionClose` | Fecha quaisquer posições vendidas existentes quando um sinal altista é recebido. |
| `Npips` | Multiplicador fracional aplicado ao trailing stop para ampliar ou reduzir a distância de reversão. |
| `PriceMode` | Variante de preço aplicada (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado, média simples, média quádrupla, seguidor de tendência ou Demark). |
| `SignalBar` | Número de velas completamente fechadas a aguardar antes de reagir a um sinal, correspondendo ao parâmetro MT5. |

## Filtros

- **Categoria**: Reversão com seguidor de tendência
- **Direção**: Bidirecional
- **Indicadores**: Stopreversal (trailing stop baseado em ATR)
- **Stops**: Stop-loss e take-profit estáticos, opcional
- **Período**: Configurável (padrão H4)
- **Sazonalidade**: Nenhum
- **Redes neurais**: Não
- **Divergência**: Não
- **Complexidade**: Moderado devido à lógica de trailing personalizada
- **Nível de risco**: Ajustável através da distância do stop e offset de trailing
