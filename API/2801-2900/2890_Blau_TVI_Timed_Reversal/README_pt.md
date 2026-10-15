# Estratégia Blau TVI de Reversão Temporizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Convertida do especialista MetaTrader 5 **Exp_BlauTVI_Tm.mq5** localizado em `MQL/21014`.
- Reimplementa a lógica do Blau Tick Volume Index (TVI) com três etapas de suavização configuráveis.
- Gera operações de reversão quando o TVI suavizado muda de inclinação e opcionalmente restringe ordens a uma sessão de trading definida pelo usuário.
- Suporta proteções opcionais de stop-loss e take-profit definidas em pontos de preço.

## Lógica do Blau Tick Volume Index
O especialista original usa o indicador personalizado `BlauTVI` que conta as subidas e descidas de volume de tick e suaviza o resultado várias vezes. O port em C# mantém a mesma ideia:

1. **Contagem bruta de ticks para cima/baixo**
   - `UpTicks = (Volume + (Close - Open) / PriceStep) / 2`
   - `DownTicks = Volume - UpTicks`
   - O volume de vela é usado como aproximação do volume de tick porque o feed do StockSharp não expõe contagens de tick para velas agregadas.
2. **Suavização Etapa 1** – `UpTicks` e `DownTicks` são suavizados com o tipo de média móvel selecionado (`EMA`, `SMA`, `SMMA`, `WMA`, `JMA`) e comprimento `Length1`.
3. **Suavização Etapa 2** – as saídas da etapa 1 são suavizadas novamente com comprimento `Length2`.
4. **Cálculo TVI** – `TVI = 100 * (Up2 - Down2) / (Up2 + Down2)`.
5. **Suavização Etapa 3** – o TVI é suavizado mais uma vez com comprimento `Length3` para reduzir ruído.

A estratégia armazena um breve histórico deslizante dos valores finais de TVI para replicar o deslocamento `SignalBar` usado pelo EA original (`CopyBuffer` com deslocamento `SignalBar`).

## Regras de Trading
- **Detecção de inclinação do sinal**
  - Quando o valor TVI anterior (`SignalBar + 1`) é menor que o valor mais antigo (`SignalBar + 2`), o TVI é considerado girando para cima. Se o último valor disponível também for maior que o anterior, um sinal de reversão de alta é produzido.
  - Quando o valor TVI anterior é maior que o valor mais antigo, o TVI está girando para baixo. Se o último valor estiver abaixo do anterior, um sinal de reversão de baixa é produzido.
- **Gestão de posições**
  - Entradas compradas requerem `EnableBuyOpen = true`, o sinal de alta acima e uma posição atual não positiva. A estratégia fecha qualquer posição vendida existente antes de comprar, adicionando o tamanho vendido absoluto ao `Volume` configurado.
  - Entradas vendidas requerem `EnableSellOpen = true`, o sinal de baixa e uma posição não negativa.
  - Saídas compradas são acionadas quando a inclinação do TVI gira para baixa e `EnableBuyClose = true`.
  - Saídas vendidas são acionadas quando a inclinação do TVI gira para alta e `EnableSellClose = true`.
- **Filtro de tempo**
  - Quando `EnableTimeFilter = true`, a estratégia só abre novas posições dentro da janela [`StartHour`:`StartMinute`, `EndHour`:`EndMinute`]. Sessões noturnas são suportadas (início > fim). Posições são fechadas à força assim que o tempo sai da sessão.
- **Proteção**
  - `StopLossPoints` e `TakeProfitPoints` são convertidos para distâncias de preço absolutas multiplicando pelo `PriceStep` do instrumento e passados para `StartProtection`. Definir como zero para desativar.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `Volume` | Tamanho da ordem usado para cada entrada (contratos adicionais são adicionados para cobrir a exposição oposta). |
| `CandleType` | Tipo/período de dados de vela usado para todos os cálculos (padrão: período de 4 horas). |
| `MaType` | Tipo de média móvel para todas as etapas de suavização (EMA, SMA, SMMA, WMA, JMA). |
| `Length1`, `Length2`, `Length3` | Comprimentos de suavização para cada etapa do cálculo Blau TVI. |
| `SignalBar` | Deslocamento para os valores TVI usados na geração de sinais (1 corresponde à vela fechada anterior como a versão MQL). |
| `EnableBuyOpen`, `EnableSellOpen` | Permitir abertura de posições compradas/vendidas em sinais. |
| `EnableBuyClose`, `EnableSellClose` | Permitir fechamento de posições compradas/vendidas existentes quando a inclinação se reverte. |
| `EnableTimeFilter` | Alternância para a janela de sessão de trading. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Limites de sessão no horário da bolsa. Suporta intervalos intradiários e noturnos. |
| `StopLossPoints`, `TakeProfitPoints` | Distâncias de proteção fixas em pontos de preço (0 desativa cada proteção). |

## Notas de Implementação
- O ambiente StockSharp não expõe contagens de tick para velas agregadas, portanto o volume de vela é usado no lugar do volume de tick. Isso mantém o comportamento próximo ao indicador original enquanto permanece compatível com os dados disponíveis.
- A estratégia rastreia apenas um histórico TVI compacto (poucos valores mais recentes) para reproduzir o deslocamento `SignalBar` sem violar a diretriz do repositório que desencoraja grandes coleções personalizadas.
- `StartProtection` é inicializado apenas quando um `PriceStep` válido está disponível; caso contrário, recorre à proteção sem alvos fixos.
- Todos os comentários foram reescritos em inglês para cumprir as regras do repositório, e tabulações são usadas para indentação conforme exigido por `AGENTS.md`.

## Dicas de Uso
1. Começar com o período padrão H4 e suavização EMA, que correspondem às configurações originais do especialista.
2. Ajustar `SignalBar` para 0 se preferir agir na última vela fechada em vez de esperar uma barra, mas lembre-se que isso desvia da lógica MQL.
3. Ao executar em instrumentos com horários de trading irregulares, configurar o filtro de tempo para evitar tomar sinais durante períodos ilíquidos.
4. Combinar com gestão de dinheiro a nível de portfólio se precisar de dimensionamento dinâmico; `Volume` é estático por design, refletindo a abordagem de lote fixo do EA fonte.
