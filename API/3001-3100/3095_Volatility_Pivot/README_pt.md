# Estratégia de Pivô de Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Pivô de Volatilidade é um port de alto nível do StockSharp do expert advisor original **Exp_VolatilityPivot.mq5**. Ela recria o indicador personalizado Volatility Pivot projetando duas linhas de stop adaptativas que seguem o preço usando volatilidade de Average True Range (ATR) ou um desvio de preço fixo. Quando a tendência muda, o indicador emite setas de rompimento de uma única barra que desencadeiam reversões de posição. A estratégia pode seguir esses sinais (`WithTrend`) ou operar contra eles (`CounterTrend`), proporcionando flexibilidade para estilos de rompimento ou reversão à média.

Ao contrário da implementação MQL, esta versão se baseia inteiramente em velas terminadas fornecidas por `CandleType`. O modo ATR multiplica um ATR suavizado (EMA do ATR) por `AtrMultiplier`, enquanto o modo de preço usa o deslocamento bruto `DeltaPrice`. As linhas de pivô resultantes definem níveis de trailing de alta e baixa que governam entradas e saídas.

## Dados de mercado e indicadores
- **Velas primárias (`CandleType`)** – todos os cálculos são realizados neste período. O padrão é uma barra de 4 horas para corresponder ao expert advisor fonte.
- **ATR + suavização EMA** – no modo `Atr` a estratégia processa um `AverageTrueRange` com comprimento `AtrPeriod` e então o suaviza por uma `ExponentialMovingAverage` de comprimento `SmoothingPeriod`.
- **Modo de desvio de preço** – no modo `PriceDeviation` o deslocamento de trailing é a quantidade fixa `DeltaPrice`, permitindo distâncias de stop determinísticas quando a suavização de volatilidade não é desejada.
- **Rastreamento do estado do pivô** – a estratégia mantém os últimos valores de trail de alta/baixa e só gera "sinais" na barra onde o trail muda de um lado do preço para o outro, espelhando os buffers do indicador da versão MQL.

## Lógica de trading
1. **Cálculo do pivô** – para cada vela terminada a estratégia atualiza o preço do stop de trailing de acordo com as regras do Volatility Pivot. Um trail de alta está ativo quando o preço fecha acima do stop calculado; um trail de baixa está ativo quando fecha abaixo.
2. **Detecção de sinais** – um novo sinal de alta (baixa) é disparado quando o trail de alta (baixa) se torna ativo depois de estar inativo na barra anterior. O parâmetro `SignalBar` atrasa a execução pelo número solicitado de barras completas, replicando a entrada `SignalBar` do script MQL.
3. **Filtro de direção (`TradeDirection`)** – quando definido como `WithTrend` a estratégia compra em sinais de alta e vende em sinais de baixa. Quando definido como `CounterTrend` a interpretação é invertida: setas de alta fecham vendidos e abrem novos vendidos, e vice-versa.
4. **Permissões de entrada** – `EnableBuyEntries` e `EnableSellEntries` controlam se novas posições compradas ou vendidas podem ser abertas.
5. **Permissões de saída** – `AllowLongExits` e `AllowShortExits` controlam se posições existentes podem ser fechadas por sinais diretos ou pelo trail oposto que permanece ativo.
6. **Ajuste de posição** – a estratégia visa uma posição líquida de `+Volume` para comprados, `-Volume` para vendidos e `0` ao zerar. As ordens são dimensionadas automaticamente para fechar qualquer exposição oposta antes de estabelecer a nova direção.
7. **Stops protetores** – distâncias opcionais de `StopLoss` e `TakeProfit` (expressas em unidades de preço absolutas) monitoram cada vela terminada. Se o máximo/mínimo da barra viola esses níveis, a estratégia sai imediatamente da posição.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Série de velas usada para processamento de indicadores e execução. | Velas de 4 horas |
| `AtrPeriod` | Comprimento do componente ATR. | 100 |
| `SmoothingPeriod` | Comprimento de suavização EMA aplicado aos valores ATR. | 10 |
| `AtrMultiplier` | Multiplicador aplicado ao ATR suavizado. | 3.0 |
| `DeltaPrice` | Deslocamento de preço fixo usado quando `PivotMode = PriceDeviation`. | 0.002 |
| `PivotMode` | Escolhe entre pivôs baseados em ATR ou desvio fixo. | `Atr` |
| `TradeDirection` | Segue (`WithTrend`) ou desvanece (`CounterTrend`) os rompimentos de pivô. | `WithTrend` |
| `SignalBar` | Número de barras completas a aguardar antes de agir sobre um sinal. | 1 |
| `EnableBuyEntries` | Permitir abertura de novas posições compradas. | `true` |
| `EnableSellEntries` | Permitir abertura de novas posições vendidas. | `true` |
| `AllowLongExits` | Permitir fechar posições compradas existentes quando condições baixistas persistem. | `true` |
| `AllowShortExits` | Permitir fechar posições vendidas existentes quando condições altistas persistem. | `true` |
| `StopLoss` | Distância de stop-loss opcional (unidades de preço absolutas). Definir como `0` para desabilitar. | 0 |
| `TakeProfit` | Distância de take-profit opcional (unidades de preço absolutas). Definir como `0` para desabilitar. | 0 |

> **Nota:** A propriedade `Strategy.Volume` do StockSharp define o tamanho da posição. Configurar antes de iniciar a estratégia para corresponder ao tamanho de contrato ou ação do instrumento.

## Diretrizes de uso
1. Anexar a estratégia ao `Security` e `Portfolio` desejados e definir `Volume` para o tamanho de lote pretendido.
2. Garantir que a fonte de dados possa fornecer o `CandleType` selecionado. Sem um feed contínuo de velas terminadas, a suavização ATR e a lógica de atraso de sinal não podem se formar.
3. Escolher `PivotMode` baseado no comportamento do mercado: o modo ATR se adapta à volatilidade, enquanto o modo de desvio de preço mantém o trail fixo.
4. Ajustar `SignalBar` para reproduzir o momento exato do expert advisor original (atraso de 1 barra por padrão). Definir como `0` para executar na barra terminada mais recente.
5. Ao usar `StopLoss`/`TakeProfit`, calibrar as distâncias à volatilidade do instrumento (são preços absolutos, não pontos ou porcentagens).
6. Monitorar os registros para mensagens informativas sobre entradas, saídas e stops protetores ativados por mudanças de pivô.

## Diferenças do expert advisor original
- As opções de gestão de dinheiro baseadas no saldo/margem livre da conta foram removidas. O tamanho da posição é controlado exclusivamente através de `Strategy.Volume`.
- O "desvio" do preço da ordem e a sincronização manual de tempo da biblioteca auxiliar MQL são desnecessários porque o StockSharp usa ordens a mercado em velas terminadas.
- Recursos de notificação, variáveis globais e carregamento manual de histórico presentes no script MQL são omitidos.
- O tratamento de stop protetor e take-profit é simplificado para verificações baseadas em velas; não há colocação de ordens intra-barra.

## Aprimoramentos recomendados
- Adicionar filtros de sessão diária ou volatilidade para pausar o trading durante horas de baixa liquidez.
- Estender a estratégia com gerenciamento de trailing-stop que espelhe as linhas de pivô, ou exportar as linhas calculadas para um gráfico para visualização.
- Incorporar controles de risco no nível do portfólio se múltiplos instrumentos usarem a mesma instância de estratégia.
