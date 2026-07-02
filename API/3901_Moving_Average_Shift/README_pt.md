# Estratégia de mudança de média móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma versão de alto nível StockSharp do clássico consultor especialista **Moving Average** que vem com MetaTrader 4. O sistema observa velas concluídas e as compara com uma média móvel simples deslocada (SMA) para detectar mudanças de direção. As ordens são sempre executadas a mercado, e a estratégia permanece no mercado com no máximo uma posição aberta por vez.

## Lógica de negociação

1. Assine velas do período configurável (padrão: 5 minutos) e calcule um SMA com o período solicitado.
2. Mude o SMA pelo número especificado de velas concluídas para emular o comportamento original da função `iMA`.
3. Avalie a vela finalizada anterior:
   - **Cruz de alta** (abertura abaixo do SMA deslocado e fechamento acima) aciona uma entrada longa quando nenhuma posição está aberta.
   - **Cruz de baixa** (abertura acima e fechamento abaixo do deslocado SMA) aciona uma entrada curta quando nenhuma posição está aberta.
4. Gerencie saídas usando as mesmas regras cruzadas:
   - Uma posição longa é fechada quando a última vela cruza abaixo do deslocado SMA.
   - Uma posição curta é fechada quando a última vela cruza acima do deslocado SMA.
5. Apenas uma posição pode existir a qualquer momento, correspondendo ao comportamento do EA original que alternava entre ordens de compra e venda.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Série de velas usadas para cálculos. Qualquer período de tempo `DataType` pode ser selecionado. | Período de 5 minutos |
| `MovingPeriod` | Número de velas para o comprimento SMA. | 12 |
| `MovingShift` | Compensação do valor SMA em velas concluídas. Emula o argumento `shift` de `iMA`. | 6 |
| `BaseVolume` | Volume de pedidos padrão para entradas. O mesmo volume é usado para negociações longas e curtas. | 1 |

## Tratamento de Indicadores

- Um indicador `SimpleMovingAverage` é criado em `OnStarted` e vinculado à assinatura da vela por meio do `Bind` API de alto nível.
- A saída SMA bruta é armazenada em buffer em uma pequena fila FIFO para obter o valor de `MovingShift` velas atrás. Nenhum recálculo manual do indicador é realizado.
- A fila retém apenas valores `MovingShift + 1`, portanto o uso de memória permanece constante mesmo para grandes turnos.

## Gerenciamento de pedidos e riscos

- Os pedidos são feitos com `BuyMarket`/`SellMarket` e dimensionados pelo parâmetro `BaseVolume`. Ao fechar, o tamanho absoluto da posição atual é usado para garantir uma saída completa.
- A implementação original do MetaTrader ajustou dinamicamente o tamanho do lote com base na margem livre e nas perdas recentes. A porta StockSharp mantém a lógica determinística e delega o dimensionamento da posição ao usuário através do parâmetro `BaseVolume`. Isso evita depender de métricas de conta específicas da corretora, preservando as regras de entrada/saída.

## Notas de conversão

- Os sinais são avaliados na vela **anterior**, correspondendo à verificação `Volume[0] == 1` de MetaTrader que esperou por uma nova barra antes de reagir.
- Apenas velas concluídas (`CandleStates.Finished`) são processadas para evitar negociações prematuras.
- A estratégia usa os auxiliares de gráfico StockSharp para traçar velas, valores de indicadores e marcadores comerciais quando uma área do gráfico está disponível.

## Uso

1. Compile a estratégia dentro do StockSharp Designer, Shell ou Runner.
2. Selecione o instrumento desejado e atribua um portfólio.
3. Configure os parâmetros se forem necessários intervalos de tempo, durações ou volumes diferentes.
4. Inicie a estratégia; ele assinará a série de velas escolhida, monitorará cruzamentos de SMA e negociará de acordo.

## Mais ideias

- Adicione stops de proteção ou níveis de take-profit usando `StartProtection` se o gerenciamento de risco além da saída de reversão básica for necessário.
- Substitua o SMA simples por outro indicador (EMA, LWMA, etc.) modificando a instância do indicador enquanto mantém o fluxo de trabalho de assinatura existente.
- Introduza regras de escala de posição ajustando o método `GetEntryVolume`.
