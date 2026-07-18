# Estratégia de Preço Extremo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Preço Extremo** replica o consultor especialista do MetaTrader `Price_Extreme_Strategy` usando a API de alto nível do StockSharp. O sistema monitora um canal deslizante derivado do maior máximo e do menor mínimo ao longo de um número configurável de velas concluídas. Sinais de rompimento são gerados sempre que a vela de referência selecionada fecha acima do limite superior ou abaixo do limite inferior. A lógica pode opcionalmente ser invertida para transformar condições de rompimento em entradas de contra-tendência.

Esta conversão mantém o fluxo de trabalho de trading orientado a eventos. As ordens são enviadas imediatamente após o fechamento de cada vela finalizada, correspondendo ao comportamento do algoritmo MQL original que reagia no tick de abertura da próxima barra.

## Lógica do indicador

O canal de Preço Extremo é reconstruído a cada vela finalizada usando os indicadores `Highest` e `Lowest` do StockSharp:

- `Highest` rastreia o máximo dos altos nas últimas *N* velas.
- `Lowest` rastreia o mínimo dos baixos nas últimas *N* velas.

Esses buffers emulam o estudo personalizado `Price_Extreme_Indicator` incluído com o consultor especialista original. O comprimento do indicador é exposto através do parâmetro **Level Length**.

Um parâmetro separado **Signal Shift** define qual vela fechada é usada para avaliar a condição de rompimento. Um shift de `1` significa "usar a vela que acabou de fechar" (padrão). Valores maiores permitem aguardar confirmação adicional referenciando barras mais antigas.

## Regras de trading

1. Recalcular os valores do canal superior e inferior para cada vela finalizada.
2. Recuperar a vela especificada por **Signal Shift** do buffer de histórico interno.
3. Gerar intenções direcionais:
   - **Rompimento para cima**: o fechamento da vela está acima do valor do canal superior.
   - **Rompimento para baixo**: o fechamento da vela está abaixo do valor do canal inferior.
4. Aplicar inversão opcional com **Reverse Signals**:
   - Se desativado, operar na direção do rompimento (comprar no rompimento para cima, vender no rompimento para baixo).
   - Se ativado, trocar as reações (vender no rompimento para cima, comprar no rompimento para baixo).
5. Respeitar as permissões **Enable Long** e **Enable Short** antes de enviar ordens.
6. Fechar automaticamente qualquer posição oposta antes de abrir uma nova negociação para que apenas uma posição líquida exista a qualquer momento.

## Gestão de risco

A estratégia fornece tratamento de stop-loss e take-profit que espelha os controles baseados em pontos da versão MQL:

- **Stop Loss** e **Take Profit** são expressos em passos de preço (`Security.PriceStep`).
- Os preços-alvo são recalculados sempre que o tamanho da posição líquida muda.
- Se uma vela finalizada ultrapassa os níveis de proteção (mínimo abaixo do stop para posições compradas, máximo acima do stop para posições vendidas, etc.), a posição é fechada por ordem a mercado e os alvos de proteção são limpos.
- `StartProtection()` é ativado durante `OnStarted` para aproveitar as salvaguardas integradas do StockSharp.

## Parâmetros

| Parâmetro | Descrição | Padrão | Grupo |
|-----------|-----------|--------|-------|
| `LevelLength` | Número de velas concluídas consideradas ao calcular o canal extremo. | 5 | Indicator |
| `SignalShift` | Índice da vela fechada usada para validação de rompimento (1 = última vela fechada). | 1 | Indicator |
| `EnableLong` | Permite comprar quando `true`. | `true` | Trading |
| `EnableShort` | Permite vender quando `true`. | `true` | Trading |
| `ReverseSignals` | Inverte reações de rompimento (comprar na queda, vender no rompimento). | `false` | Trading |
| `OrderVolume` | Volume enviado com cada ordem a mercado. Deve ser maior que zero. | 1 | Trading |
| `StopLossPoints` | Distância do stop-loss medida em passos de preço. Um valor de `0` desativa o stop. | 0 | Risk |
| `TakeProfitPoints` | Distância do take-profit medida em passos de preço. Um valor de `0` desativa o alvo. | 0 | Risk |
| `CandleType` | Período principal para assinatura de dados. | Velas de 5 minutos | Data |

Todos os parâmetros usam `StrategyParam<T>` com metadados de UI para que possam ser otimizados ou modificados no Designer.

## Diretrizes de uso

1. Anexar a estratégia a um instrumento e definir o **Candle Type** para corresponder ao período usado na configuração original do MetaTrader.
2. Ajustar **Level Length** se um canal de Preço Extremo mais largo ou mais estreito for desejado.
3. Configurar **Signal Shift** para controlar quantas velas fechadas aguardar antes de avaliar o rompimento.
4. Selecionar as direções de negociação desejadas via **Enable Long**, **Enable Short** e **Reverse Signals**.
5. Definir **Order Volume**, **Stop Loss** e **Take Profit** de acordo com as preferências de risco. Lembre-se que ambos os valores de proteção operam em passos de preço.
6. Iniciar a estratégia. Velas, bandas do indicador e negociações executadas são plotadas automaticamente quando uma área de gráfico está disponível.

## Notas adicionais

- A estratégia opera intencionalmente em uma única posição líquida, espelhando a lógica de hedge do especialista MQL ao nivelar o lado oposto antes de entrar em uma nova negociação.
- Stops e alvos de proteção são avaliados em velas concluídas. No trading ao vivo, isso aproxima as ordens de proteção do lado do servidor usadas pelo script original.
- Nenhuma versão em Python está incluída, conforme solicitado.
