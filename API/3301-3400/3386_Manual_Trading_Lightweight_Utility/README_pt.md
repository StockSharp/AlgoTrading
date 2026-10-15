# Estratégia de utilidade leve de negociação manual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O consultor especialista original "Manual Trading Lightweight Utility" é um painel MetaTrader compacto que expõe botões para alternar entre ordens de mercado, limite e stop, ajusta volumes independentemente para ações de compra e venda e anexa automaticamente compensações de stop-loss e take-profit. Esta porta C# recria o mesmo fluxo de trabalho dentro de StockSharp representando cada botão do painel como um parâmetro de estratégia. A estratégia não produz sinais autónomos; ele aguarda suas instruções manuais e, em seguida, executa a ação solicitada usando o API de alto nível enquanto supervisiona as saídas de proteção.

## Funcionalidade recriada
- **Solicitações únicas de compra e venda.** Duas alternâncias booleanas emulam os botões do painel. Definir `BuyRequest` ou `SellRequest` como `true` aciona exatamente uma ordem de mercado, limite ou stop com base no modo selecionado e redefine imediatamente a alternância para `false`.
- **Preços pendentes automáticos ou manuais.** Cada lado pode reutilizar as compensações de MetaTrader (`LimitOrderPoints` e `StopOrderPoints`) ou aceitar um preço absoluto manual. A precificação automática usa o melhor lance/venda atual ou o último fechamento da vela quando as cotações não estão disponíveis.
- **Volumes independentes.** Você pode compartilhar um volume padrão entre ambos os lados ou ativar volumes por lado para espelhar a opção de controle de lote da versão MQL.
- **Proteção baseada em pontos.** `TakeProfitPoints` e `StopLossPoints` traduzem as distâncias de MetaTrader pontos em compensações de preço usando o instrumento `PriceStep`. A estratégia monitora velas concluídas e fecha a posição com uma ordem de mercado quando um nível de proteção é ultrapassado.
- **Comente comentários.** Cada ação manual grava uma entrada de registro que inclui o `OrderComment` configurado, facilitando o acompanhamento dos comandos executados sem um painel visual.

## Fluxo de estratégia
1. A estratégia segue o tipo de vela selecionado por `CandleType`. As velas finalizadas fornecem os preços de referência utilizados para compensações e supervisão de risco.
2. Para cada vela concluída a estratégia:
   - Atualiza a classe base `Volume` com `DefaultVolume` (útil para inspeção visual em StockSharp).
   - Detecta alterações em `BuyRequest` e `SellRequest` e as marca como ações pendentes.
   - Assim que os dados de mercado estiverem prontos (`IsFormedAndOnlineAndAllowTrading()`), executa as ações solicitadas, resolve os preços das ordens pendentes e registra o resultado.
   - Chama o gestor de risco que regista o preço de entrada sempre que a posição líquida muda e emite saídas de mercado se os limites de stop-loss ou take-profit forem ultrapassados.
3. Quando a posição retorna para plana, todo o estado interno é redefinido para que a próxima solicitação manual comece do zero.

## Parâmetros
- **`CandleType`** – série de dados de mercado utilizada para referências de preços e gerenciamento de risco.
- **`BuyOrderMode` / `SellOrderMode`** – escolha entre `MarketExecution`, `PendingLimit` ou `PendingStop` para cada lado.
- **`UseAutomaticBuyPrice` / `UseAutomaticSellPrice`** – habilite o preço de compensação automática. Desative para fornecer um preço absoluto fixo.
- **`BuyManualPrice` / `SellManualPrice`** – preços de pedidos pendentes manuais aplicados quando o preço automático está desativado (definido como `0` para ignorar).
- **`DefaultVolume`** – volume de pedido compartilhado quando volumes individuais estão desativados.
- **`UseIndividualVolumes`** – alterna o análogo do Controle de lote. Quando ativado, os próximos dois parâmetros substituem o volume compartilhado.
- **`BuyVolume` / `SellVolume`** – volumes por lado.
- **`TakeProfitPoints` / `StopLossPoints`** – distâncias de proteção expressas em MetaTrader pontos. Zero desativa o respectivo recurso.
- **`LimitOrderPoints` / `StopOrderPoints`** – compensações aplicadas aos preços limite e stop automáticos, também medidas em pontos.
- **`BuyRequest` / `SellRequest`** – alternâncias momentâneas que emulam os botões do painel. Eles são redefinidos automaticamente após o processamento da solicitação.
- **`OrderComment`** – texto de formato livre anexado ao log quando uma ação é executada.

## Diretrizes de uso
1. Configure `CandleType` para corresponder à granularidade que você deseja usar para compensações e verificações de risco. O período padrão de um minuto se assemelha ao comportamento orientado por ticks do script MetaTrader, mantendo-se compatível com backtests históricos.
2. Escolha se deseja trabalhar com um único `DefaultVolume` ou permitir que o `UseIndividualVolumes` controle os volumes de compra e venda separadamente. Os volumes devem permanecer positivos.
3. Decida como os preços pendentes devem ser calculados. Deixe `UseAutomatic*Price` ativado para replicar os deslocamentos de ponto MetaTrader ou desative-o e forneça valores `BuyManualPrice` / `SellManualPrice` explicitamente.
4. Defina `TakeProfitPoints` e `StopLossPoints` conforme necessário. Quando são maiores que zero, a estratégia os converte em distâncias de preço usando o instrumento `PriceStep` e fecha a posição com uma ordem de mercado assim que uma vela ultrapassa o limite relevante. Se o símbolo não tiver um `PriceStep` configurado, um aviso será registrado e as distâncias de proteção serão ignoradas.
5. Para enviar um pedido, altere `BuyRequest` ou `SellRequest` de `false` para `true`. A estratégia resolve a solicitação na próxima vela finalizada, envia o tipo de pedido escolhido, grava uma entrada de log e zera o sinalizador para que a ação não seja repetida automaticamente.
6. Emita novamente qualquer ação alternando novamente o parâmetro correspondente. As solicitações permanecem ociosas se o preço requerido não puder ser resolvido (por exemplo, porque um preço manual é zero); corrija a configuração e alterne novamente para tentar novamente.

## Diferenças do utilitário MQL original
- Os objetos gráficos MetaTrader são substituídos por parâmetros StockSharp. Cada botão e alternância do painel original agora é uma propriedade editável que pode ser controlada pela interface do usuário ou por meio de scripts de automação.
- Os níveis de proteção são executados com ordens de mercado quando violados, em vez de registrar ordens de proteção stop/limit separadas. Isso mantém a implementação dentro do API de alto nível e evita o gerenciamento manual dos ciclos de vida dos pedidos.
- Os preços automáticos voltam para o último fechamento da vela se as melhores cotações de compra/venda não estiverem disponíveis, garantindo um comportamento determinístico durante os backtests, onde os dados do livro de pedidos podem estar ausentes.

## Notas
- A estratégia armazena o preço de entrada sempre que a posição líquida muda. Se você entrar em uma negociação, as compensações de proteção serão ancoradas novamente no fechamento da vela que reflete o novo tamanho.
- A compensação de spread é incluída no cálculo de stop loss adicionando o spread mais conhecido (ou uma etapa de preço quando faltam cotações) à distância do ponto configurada, espelhando a lógica MQL que ampliou as paradas de venda pelo spread atual.
- As entradas de log contêm comentários configurados, tipo de pedido, preço (para pedidos pendentes) e volume, fornecendo uma trilha de auditoria concisa para cada ação manual.
