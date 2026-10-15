# Estratégia de média da Amstell SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conversão do consultor especialista MetaTrader `exp_Amstell-SL`. O sistema abre posições longas e curtas imediatamente, adiciona novas ordens sempre que o preço se move em relação à entrada mais recente por um número fixo de pontos e depende de níveis virtuais de take-profit e stop-loss (gerenciados por software) para sair de cada ticket individualmente.

## Lógica da estratégia

- **Entradas Iniciais**: Quando a estratégia é iniciada e não há negociações abertas, ela envia uma compra de mercado (com venda) e uma venda de mercado (com oferta).
- **Pirâmide no rebaixamento**:
  - Lado longo: sempre que o pedido atual estiver `ReentryPoints` (padrão 10 pontos) abaixo do último preço de entrada comprado, uma nova ordem de compra do mesmo volume é enviada.
  - Lado vendido: sempre que o lance atual estiver `ReentryPoints` acima do último preço de entrada vendido, uma nova ordem de venda do mesmo volume é aberta.
- **Regras de saída (gerenciamento virtual)**:
  - Para cada ticket longo, a estratégia monitora o melhor lance e o melhor pedido. Se o lance aumentar em `TakeProfitPoints` em relação ao preço da ordem ou o pedido cair em `StopLossPoints`, a posição será fechada no mercado.
  - Para cada ticket curto ele verifica se o ask é menor em `TakeProfitPoints` ou se o lance é maior em `StopLossPoints`; em ambos os casos, a ordem de venda é coberta pelo mercado.
- **Ordem de Processamento**: As saídas são avaliadas antes de qualquer nova entrada, replicando o script MetaTrader que interrompe ações futuras após fechar uma posição no tick atual.

## Parâmetros

- `TakeProfitPoints` – distância (em etapas de preço) usada para fechar posições lucrativas. Padrão: `30`.
- `StopLossPoints` – distância (em etapas de preço) para saídas de proteção. Padrão: `30`.
- `Volume` – tamanho do lote para cada pedido recém-aberto. Padrão: `0.01`.
- `ReentryPoints` – movimento adverso (em etapas de preço) necessário para empilhar uma ordem adicional no lado correspondente. Padrão: `10`.

## Notas adicionais

- O valor do ponto é derivado de `Security.PriceStep`; se não for fornecido pela exchange, um valor de `1` será usado.
- A estratégia pode ser simultaneamente comprada e vendida porque rastreia a compra e a venda de ingressos de forma independente, correspondendo ao comportamento de estilo de hedge do consultor especialista original.
- Os níveis de take-profit e stop-loss são executados virtualmente por ordens de mercado; eles não são colocados no livro de ordens da bolsa.
- O risco aumenta rapidamente quando os preços tendem fortemente em uma direção porque pedidos adicionais são abertos sem reduzir a exposição anterior.
- Funciona melhor em símbolos onde a noção de "ponto" é igual a um incremento mínimo de preço, por exemplo, grandes pares de forex com preços no estilo MetaTrader.
