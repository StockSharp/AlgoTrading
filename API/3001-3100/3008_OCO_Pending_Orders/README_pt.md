# Estratégia de Ordens Pendentes OCO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Ordens Pendentes OCO** replica o comportamento do consultor especialista MetaTrader4 `OCO_EA.mq4` dentro da API de alto nível do StockSharp. O algoritmo permite que um trader ative até quatro gatilhos de preço independentes (buy limit, buy stop, sell limit, sell stop). Sempre que o melhor bid ou ask ao vivo toca o nível de preço configurado, a estratégia envia uma ordem de mercado imediata, cancelando opcionalmente todos os outros gatilhos pendentes de forma clássica "one-cancels-the-others" (OCO).

A estratégia depende puramente de dados de mercado de nível 1 – nenhum indicador histórico é necessário. É destinada a fluxos de trabalho de negociação discricionária ou semi-automatizada onde os traders definem manualmente os níveis de preço e querem que a plataforma execute assim que o nível seja atingido, enquanto também anexa ordens de saída protetoras.

## Lógica de negociação
1. O trader define qualquer combinação dos quatro preços de gatilho e muda o parâmetro **Armed** para `true`.
2. A estratégia se inscreve em atualizações de nível 1 e mantém o último melhor bid e ask na memória.
3. Em cada atualização compara os preços armazenados com os limiares configurados:
   - Se o melhor ask for *menor ou igual ao* preço **Buy limit**, uma ordem de compra de mercado com o volume configurado é enviada.
   - Se o melhor ask for *maior ou igual ao* preço **Buy stop**, uma ordem de compra de mercado é enviada.
   - Se o melhor bid for *maior ou igual ao* preço **Sell limit**, uma ordem de venda de mercado é enviada.
   - Se o melhor bid for *menor ou igual ao* preço **Sell stop**, uma ordem de venda de mercado é enviada.
4. Após cada gatilho executado, o nível correspondente é apagado (redefinido para zero). Quando **Use OCO link** está habilitado, todos os outros níveis são apagados imediatamente, espelhando o comportamento original do MT4. Quando o link OCO está desabilitado, outros níveis permanecem ativos até que disparem ou sejam apagados manualmente.
5. Se todos os preços de gatilho forem zero, a estratégia se desarma automaticamente mudando **Armed** de volta para `false`.

Todas as execuções são realizadas com chamadas `BuyMarket` e `SellMarket` para garantir preenchimentos imediatos que respeitem o roteamento de câmbio configurado no ambiente StockSharp. Entradas de registro informativas são produzidas para cada gatilho para simplificar o monitoramento.

## Parâmetros
- **Order volume** – volume enviado com cada ordem de mercado. O valor deve ser positivo.
- **Buy limit price** – limiar de preço ask que ativa uma entrada comprada no estilo limite. Definir como `0` para desabilitar.
- **Buy stop price** – limiar de preço ask que ativa uma entrada comprada no estilo stop. Definir como `0` para desabilitar.
- **Sell limit price** – limiar de preço bid que ativa uma entrada vendida no estilo limite. Definir como `0` para desabilitar.
- **Sell stop price** – limiar de preço bid que ativa uma entrada vendida no estilo stop. Definir como `0` para desabilitar.
- **Stop loss (pips)** – distância em pontos do instrumento para o stop de proteção. Convertido para preço multiplicando por `Security.PriceStep` (fallback `1` quando o instrumento não reporta um tamanho de tick).
- **Take profit (pips)** – distância em pontos do instrumento para o objetivo de lucro. A mesma lógica de conversão do stop loss é usada.
- **Use OCO link** – se `true`, a primeira ordem preenchida apaga os níveis de preço restantes e desarma a estratégia. Se `false`, os níveis restantes permanecem ativos até que disparem individualmente.
- **Armed** – interruptor de segurança que habilita ou desabilita a lógica de negociação. A estratégia o redefine automaticamente para `false` quando não há mais níveis de gatilho ativos.

## Gestão de risco
`StartProtection` é habilitado durante `OnStarted`, anexando offsets de stop-loss e take-profit de preço absoluto a cada posição aberta. Os offsets são derivados dos parâmetros **Stop loss (pips)** e **Take profit (pips)** usando o tamanho do tick do instrumento. As ordens de proteção são sempre enviadas como ordens de mercado para garantir a execução da saída mesmo quando o instrumento subjacente é ilíquido.

Como a estratégia é puramente orientada a eventos, ela não mantém ordens limite pendentes na bolsa; ela reage a dados de mercado e envia ordens de mercado, assim como a versão MQL original que emitia ordens imediatas e depois as modificava para aplicar as distâncias de stop-loss e take-profit.

## Dicas de uso
1. Configurar o instrumento, portfólio e conexão dentro do StockSharp como de costume.
2. Definir **Order volume** para corresponder ao tamanho de lote desejado.
3. Inserir qualquer subconjunto de preços de gatilho e mudar **Armed** para `true`. Valores deixados em `0` são ignorados.
4. Opcionalmente desabilitar **Use OCO link** para manter os gatilhos restantes ativos após o primeiro preenchimento.
5. Monitorar o registro para mensagens confirmando cada gatilho e o estado de redefinição automática.

Lembre-se que a estratégia usa o passo de preço fornecido pelo corretor. Se o instrumento de negociação cota em pips fracionários ou usa tamanhos de tick não convencionais, ajuste as distâncias baseadas em pips de acordo antes de armar a estratégia.

## Diferenças em relação ao script MQL original
- A estratégia usa o auxiliar `StartProtection` do StockSharp em vez de modificar manualmente as ordens para aplicar níveis de stop-loss e take-profit.
- As inscrições de dados de nível 1 são tratadas através de bindings de alto nível em vez de polling manual dos valores `Bid`, `Ask`, `High` e `Low`.
- Os parâmetros são expostos através de `StrategyParam<T>` para que possam ser ajustados e otimizados diretamente na UI do StockSharp.
- O registro substitui as notificações `Comment` e `PlaySound` do MT4, fornecendo transparência de execução dentro dos registros do StockSharp.
