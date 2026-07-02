# Estratégia VR Smart Grid Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia VR Smart Grid Lite** replica a lógica do consultor especialista MetaTrader com o mesmo nome. A estratégia constrói uma grade média no estilo martingale usando ordens de mercado. O dimensionamento da posição começa a partir de um volume base e duplica cada vez que o preço se move em relação à posição existente por uma distância definida pelo usuário. A estratégia suporta dois modos de saída: fechar as negociações extremas a um preço de lucro ponderado ou reduzir parcialmente a exposição, mantendo a rede ativa.

## Parâmetros
- **Take Profit (pips)** – distância em pips usada para sair quando apenas uma posição está ativa.
- **Volume inicial** – volume inicial do pedido para a primeira negociação em cada direção.
- **Volume Máximo** – limite máximo para qualquer pedido único aberto pela grade.
- **Modo Fechar** – `Average` fecha os pedidos mais antigos e mais novos em uma meta ponderada; `PartClose` fecha parte do pedido mais recente e todo o pedido mais antigo.
- **Etapa da Ordem (pips)** – distância mínima do preço que deve ser percorrida em relação à posição antes de uma nova negociação ser aberta.
- **Lucro Mínimo (pips)** – margem de lucro adicional adicionada ao preço médio ponderado de saída.
- **Slippage (pips)** – parâmetro de espaço reservado retido do EA original para fins de integridade.
- **Tipo de vela** – período de tempo usado para orientar a tomada de decisão (a vela concluída anteriormente determina o viés de negociação).

## Algoritmo
1. Em cada vela finalizada, a estratégia avalia a direção da vela anterior.
2. Se a vela anterior fechou em alta e não existem negociações longas ou o preço caiu na etapa configurada, uma nova ordem de **mercado de compra** será colocada.
3. Se a vela anterior fechou em baixa e não existem negociações curtas ou o preço subiu na etapa configurada, uma nova ordem de **mercado de venda** será colocada.
4. Os volumes são calculados a partir da posição de menor preço na direção e duplicados a cada novo nível, respeitando os passos de volume máximo e volume da corretora.
5. Quando resta apenas uma posição, a estratégia aplica a distância simples de take-profit e sai no toque.
6. Com múltiplas posições, a estratégia calcula médias ponderadas utilizando as entradas extremas:
   - **Modo médio** fecha ambos os extremos quando o preço atinge a meta ponderada mais o buffer de lucro mínimo.
   - **Modo PartClose** fecha uma parte do pedido mais recente igual ao volume inicial e fecha totalmente o pedido mais antigo, permitindo que a grade continue funcionando com exposição reduzida.
7. Todas as posições preenchidas e fechadas são rastreadas para manter o estado da grade interna sincronizado com o portfólio ativo.

## Notas
- A estratégia depende de ordens de mercado, portanto a qualidade real de execução e a derrapagem dependem das condições da corretora.
- Certifique-se de que as restrições de volume do instrumento (volume mínimo e passo de volume) sejam compatíveis com o volume inicial selecionado.
- Tal como acontece com qualquer abordagem de grelha ou martingale, o risco pode aumentar rapidamente quando os mercados tendem fortemente contra a posição; usar uma gestão prudente do dinheiro.
