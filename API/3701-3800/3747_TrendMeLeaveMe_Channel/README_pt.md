# Estratégia de canal pendente TrendMeLeaveMe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta implementação StockSharp recria o consultor especialista MetaTrader "TrendMeLeaveMe" original. A ideia é seguir manualmente um canal de tendência dinâmico e usar ordens de stop pendentes para capturar rompimentos sempre que o preço abraçar a linha de tendência. Como StockSharp não funciona com objetos gráficos desenhados pelo usuário, a estratégia reconstrói o centro do canal automaticamente com um indicador de regressão linear e, em seguida, reproduz a mesma lógica de deslocamento que a versão MQL aplicou às linhas guias superiores e inferiores.

A abordagem é projetada para entradas longas e curtas. Assim que uma ordem de stop é acionada, a posição é imediatamente protegida com ordens estáticas de stop-loss e take-profit que refletem as distâncias configuradas no EA. Os pedidos pendentes são constantemente atualizados para que os níveis de ativação rastreiem o valor mais recente da linha de regressão.

## Como funciona a estratégia

1. Uma assinatura de vela aciona um indicador `LinearRegression` que atua como a linha de tendência intermediária.
2. O usuário define quatro compensações (superior/inferior para cenários de compra e venda) nas etapas de preço do instrumento. A estratégia traduz-os em preços acima ou abaixo da linha de regressão.
3. Quando a última vela fecha entre a linha de tendência e o deslocamento inferior configurado, um stop de compra é posicionado no deslocamento superior. Simetricamente, quando o preço fecha entre a linha e o deslocamento superior, um stop de venda é colocado no limite inferior.
4. Se o mercado sair dessas zonas de ativação, a ordem pendente correspondente será cancelada para que a estratégia não sobrecarregue o livro.
5. Após a execução de uma ordem de stop, a negociação é encerrada com um stop loss estático e um take-profit que usa as mesmas distâncias de pontos do consultor especialista original.

## Sinais

- **Configuração de compra**: O fechamento da vela está abaixo ou igual à linha de regressão, mas ainda acima do deslocamento inferior de compra. Uma ordem stop de compra é colocada no deslocamento superior e segue a linha enquanto a condição permanece válida.
- **Configuração de venda**: O fechamento da vela está acima ou igual à linha de regressão, mas ainda abaixo do deslocamento superior de venda. Uma ordem stop de venda é colocada no deslocamento inferior e segue a linha de tendência.
- **Sem configuração**: Quando o preço está fora do corredor de ativação, os pedidos pendentes existentes são removidos.

## Gestão de risco

- As negociações de compra usam `BuyStopLossSteps` e `BuyTakeProfitSteps` para calcular níveis fixos de stop-loss e take-profit a partir do preço de entrada.
- As negociações de venda usam `SellStopLossSteps` e `SellTakeProfitSteps` para a mesma finalidade.
- As ordens de proteção são recalculadas apenas quando a posição líquida muda, imitando como MetaTrader atribui níveis de stop diretamente a cada ordem pendente.

## Parâmetros

- `CandleType` – agregação de velas usada para calcular a linha de tendência.
- `TrendLength` – número de velas na janela de regressão linear.
- `BuyStepUpper` / `BuyStepLower` – compensações (em etapas de preço) que definem o gatilho superior e o limite de ativação inferior para configurações longas.
- `SellStepUpper` / `SellStepLower` – compensações (em etapas de preço) que definem o corredor de ativação para configurações curtas.
- `BuyTakeProfitSteps` / `BuyStopLossSteps` – distâncias para saídas de posições longas, expressas em etapas de preço.
- `SellTakeProfitSteps` / `SellStopLossSteps` – distâncias para saídas de posições curtas.
- `BuyVolume` / `SellVolume` – volume utilizado para ordens pendentes de cada lado.

## Notas

- Como não há linhas de tendência manuais, o indicador de regressão substitui os objetos gráficos da estratégia MQL. Os usuários podem experimentar o comprimento da regressão para aproximar sua análise manual de tendências.
- A estratégia só negocia quando a conexão da exchange está ativa (`IsFormedAndOnlineAndAllowTrading`).
- As ordens pendentes são canceladas automaticamente sempre que já existir uma posição na mesma direção, reproduzindo o comportamento de ordem única do EA original.
