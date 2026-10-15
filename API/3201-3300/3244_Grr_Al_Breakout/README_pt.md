# Estratégia de Grr Al Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Grr Al Breakout** é um port direto do consultor especialista MetaTrader `grr-al.mq5`. Observa o primeiro preço atingido no início de cada vela e aguarda que o mercado se mova uma distância configurável desse nível de âncora. Quando o movimento excede o limiar, a estratégia executa exatamente uma operação para aquela vela, opcionalmente revertendo a exposição existente.

A implementação StockSharp mantém o comportamento do robô original orientado por timer, mas o traduz para o modelo de subscrição de velas de alto nível. Cada novo snapshot de vela fornece o preço de referência inicial, enquanto as atualizações subsequentes da mesma vela fornecem o último fechamento usado como preço de mercado ao vivo. Esta abordagem recria a detecção de rompimento tick a tick sem depender do processamento de eventos de baixo nível.

## Lógica de trading
1. **Detecção de âncora** – quando uma nova vela começa, a estratégia armazena seu preço de abertura (ou o primeiro fechamento disponível se a abertura ainda não estiver disponível) e redefine o gatilho por vela.
2. **Verificação de rompimento** – enquanto nenhuma operação foi executada durante a vela atual, o último fechamento é comparado com a âncora. Se o preço subir mais de `DeltaPoints` (convertido em preço pelo tamanho do ponto do instrumento), uma posição vendida é aberta. Se o preço cair a mesma distância, uma posição comprada é aberta.
3. **Execução única por vela** – uma vez que uma operação de rompimento é disparada, nenhuma ordem adicional é permitida até que a próxima vela comece, imitando o flag `br` do EA original.
4. **Gerenciamento de risco** – distâncias opcionais de stop-loss e take-profit são aplicadas imediatamente após abrir uma posição. Se a ordem apenas reduz uma exposição oposta, os brackets de proteção são ignorados para evitar anexar stops a um portfólio zerado.
5. **Dimensionamento de posição** – a estratégia pode negociar com um volume fixo ou limitar o tamanho da ordem a uma fração do volume máximo reportado pelo broker.

## Parâmetros
- `Volume` – volume base (em contratos) usado quando `RiskFraction` é zero. Corresponde à constante `BASELOT` da versão MQL.
- `RiskFraction` – valor entre 0 e 1. Se maior que zero, a estratégia limita o tamanho da ordem multiplicando o volume máximo do broker por esta fração e usa o menor valor entre esse limite e `Volume`.
- `DeltaPoints` – número de pontos do instrumento que o preço deve se mover da abertura da vela para acionar uma operação. Equivalente à constante `DELTA`.
- `StopLossPoints` – distância de stop protetor em pontos. Zero desabilita o stop, assim como a constante `SL` sendo zero no MQL.
- `TakeProfitPoints` – distância de take-profit em pontos. Zero desabilita o alvo e replica o comportamento da constante `TP`.
- `CandleType` – descritor de vela StockSharp que define o período para ancoragem e monitoramento de rompimentos. Por padrão usa período de cinco minutos, mas pode ser alterado para qualquer período suportado.

## Notas e diferenças em relação à versão MQL
- O EA original usava eventos de tick com um timer de um segundo. Este port utiliza a API de subscrição de velas do StockSharp, que automaticamente fornece o último estado da vela; nenhum gerenciamento manual de timer é necessário.
- A diferenciação bid/ask não está disponível na interface de alto nível, portanto a estratégia usa o fechamento da vela como proxy para o preço de negociação. Os offsets de stop-loss e take-profit ainda são aplicados em pontos, correspondendo ao comportamento da aritmética de pontos do MetaTrader.
- O cálculo de volume baseado em risco no MetaTrader dependia da estimativa de margem para uma ordem de um lote fixo. Neste port, o cálculo é simplificado para uma fração do volume máximo para que permaneça agnóstico ao broker.
- Como as estratégias do StockSharp são baseadas em posição líquida, enviar uma ordem na direção oposta pode zerar ou reverter a exposição automaticamente, similar à chamada `OrderSend` com modo netting no MetaTrader 5.

## Uso
1. Anexar a estratégia a um ativo e portfólio no Designer, Runner ou uma aplicação host personalizada StockSharp.
2. Configurar o período de vela desejado, distância de rompimento, stop-loss, take-profit e parâmetros de volume.
3. Iniciar a estratégia. Ela subscreverá automaticamente às velas escolhidas, monitorará cada nova vela para um movimento de rompimento e colocará ordens de mercado quando as condições configuradas forem atendidas.

## Fonte original
- Consultor especialista MetaTrader 5: `MQL/244/grr-al.mq5`
