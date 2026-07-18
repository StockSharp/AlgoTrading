# Estratégia de painel utilitário leve de negociação manual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia do painel utilitário leve de negociação manual** replica o comportamento do painel MT4 "Utilitário leve de negociação manual" usando a estratégia de alto nível StockSharp API. Ele expõe os mesmos controles interativos que os parâmetros de estratégia para que o operador possa alternar entre ordens de mercado, limite e stop, ajustar o cálculo automático de preços, configurar o gerenciamento de volume e anexar controles de risco sem depender de objetos gráficos personalizados.

A estratégia é projetada para negociação discricionária. Os pedidos são acionados manualmente alterando os parâmetros `Send Buy Order` ou `Send Sell Order` na IU. Cada comando é reconhecido imediatamente, enquanto a estratégia mantém todos os cálculos — como sugestões automáticas de preços e níveis de risco — sincronizados com dados de mercado em tempo real.

## Principais recursos
- **Envio manual de pedidos** para compradores e vendedores, com suporte para ordens de mercado, limite e stop.
- **Sugestão automática de preço** que reflete a lógica do painel MT4, atualizando o limite proposto ou o preço stop do último fluxo de compra/venda.
- **Modo de preço manual opcional** que permite ao operador digitar o nível de disparo desejado respeitando os tamanhos dos passos do instrumento.
- **Gerenciamento de volume** com tamanho de lote global e volumes de compra/venda individuais quando a chave de controle de lote está habilitada.
- **Gerenciamento integrado de stop-loss e take-profit** implementado na camada de estratégia para emular proteções anexadas a pedidos no MT4.
- **Feedback detalhado** por meio de parâmetros que sempre refletem os últimos níveis de entrada calculados para ambos os lados.

## Notas de conversão
- Os objetos gráficos MT4 (botões, rótulos e caixas de edição) são substituídos por parâmetros de estratégia agrupados em seções lógicas para fácil acesso no Hydra/Terminal.
- As paradas e metas de proteção são tratadas internamente, observando o preço de mercado ao vivo porque StockSharp não os incorpora em ordens pendentes da mesma forma que o MT4.
- As compensações de preços expressas em pontos reutilizam os metadados do instrumento (`PriceStep` e `VolumeStep`) para que os limites e paradas sempre respeitem as restrições cambiais.

## Uso
1. Anexe a estratégia a um título e portfólio no Hydra ou Terminal.
2. Configure o tamanho do lote padrão, parâmetros de risco e compensações de preço.
3. Opcionalmente, habilite `Lot Control` para manter volumes independentes para os botões de compra e venda.
4. Escolha o tipo de ordem (mercado, limite pendente ou stop pendente) e se o preço de gatilho deve seguir o mercado ou permanecer manual.
5. Quando estiver pronto, alterne `Send Buy Order` ou `Send Sell Order` para `true`. A estratégia enviará o pedido correspondente e redefinirá o sinalizador para `false` depois de processado.
6. O gestor de proteção fechará as posições abertas nos níveis de stop-loss ou take-profit configurados, calculados a partir do preço de entrada executado.

## Arquivos
- `CS/ManualTradingLightweightUtilityPanelStrategy.cs` – Implementação da estratégia em C#.
- `README.md` – Documentação em inglês (este arquivo).
- `README_zh.md` – Documentação em chinês simplificado.
- `README_ru.md` – Documentação russa.
