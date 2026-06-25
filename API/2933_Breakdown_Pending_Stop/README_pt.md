# Estratégia de Rompimento com Stop Pendente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia recria o consultor especialista original de "breakdown" do MetaTrader. Coloca ordens de stop ao redor do intervalo do dia anterior e atualiza continuamente as ordens a cada sessão. Um motor de trailing-stop replica a lógica de trailing escalonado do script fonte, mantendo os stops ajustados uma vez que a posição começa a mover na direção lucrativa.

## Como Funciona
- **Preparação diária** – Quando uma vela diária fecha, a estratégia armazena o máximo e o mínimo. No início da sessão seguinte cancela as ordens restantes e envia um buy stop acima do máximo anterior e um sell stop abaixo do mínimo anterior. O parâmetro `Min Distance (ticks)` desloca as ordens dos níveis brutos para evitar ruído.
- **Atualização de ordens** – Sempre que as ordens pendentes são executadas ou um novo dia começa, as ordens restantes são canceladas e um novo par é enviado usando os mesmos níveis do dia anterior. O comportamento espelha o especialista MQL que mantém continuamente entradas de stop em ambos os lados do mercado.
- **Controles de risco** – Posições executadas inicializam alvos de stop-loss e take-profit com base em distâncias em ticks. Uma regra de trailing escalonado sobe/baixa o stop apenas depois que o preço ganha pelo menos `Trailing Stop (ticks) + Trailing Step (ticks)` desde a entrada, exatamente como a implementação original de trailing-stop.
- **Saídas** – As posições fecham imediatamente quando o preço toca o stop ou o alvo ativo. O trailing manual fecha posições ao mercado quando o nível de trailing é violado, coincidindo com a lógica do MetaTrader que modificava stops a cada tick.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `Working Candles` | Período usado para monitorar a ação do preço e gerenciar stops (padrão: velas de 15 minutos). |
| `Stop Loss (ticks)` | Distância do stop protetor inicial convertida em preço absoluto usando o tamanho do tick do instrumento. Definir como zero para desabilitar. |
| `Take Profit (ticks)` | Distância inicial de take-profit. Definir como zero para desabilitar. |
| `Trailing Stop (ticks)` | Distância principal do trailing-stop. Definir como zero para desabilitar o trailing. |
| `Trailing Step (ticks)` | Lucro adicional necessário antes que o trailing stop seja movido. |
| `Min Distance (ticks)` | Offset adicionado ao máximo/mínimo do dia anterior ao colocar as ordens pendentes. |
| `Order Volume` | Quantidade enviada com ambas as ordens de stop. |

## Notas de Uso
- Configurar a estratégia em instrumentos que publiquem velas diárias para que o intervalo da sessão anterior possa ser obtido.
- A lógica assume um tamanho de tick constante. Para instrumentos com incrementos de tick variáveis, ajustar os padrões adequadamente.
- A estratégia não implementa o dimensionamento baseado em porcentagem do script MQL original; o volume é definido explicitamente através do parâmetro `Order Volume`.
- Nenhuma versão Python ainda é fornecida para esta estratégia.
