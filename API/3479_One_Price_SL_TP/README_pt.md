# Estratégia Stop-Loss / Take-Profit de um preço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de utilidade replica o script MetaTrader "One Price SL TP" dentro de StockSharp. Em vez de abrir negociações, o algoritmo observa a posição atual no instrumento configurado e garante que ambas as ordens de proteção estejam alinhadas com um único preço-alvo especificado pelo usuário.

Sempre que o parâmetro **`ZenPrice`** estiver acima de zero, a estratégia o compara com as cotações de compra/venda em tempo real:

- Para uma posição **longa**: se `ZenPrice` for superior ao pedido, uma ordem com limite de lucro será colocada a esse preço; se `ZenPrice` for inferior ao lance, uma ordem stop-loss será registrada.
- Para uma posição **curta**: se `ZenPrice` for inferior ao lance, torna-se a ordem com limite de lucro; se `ZenPrice` for superior ao pedido, torna-se a ordem stop-loss.

Quando o preço fica entre o lance e o pedido, nada é enviado, portanto a ordem de proteção anterior permanece intacta. Assim que a posição for fechada ou o parâmetro for redefinido para zero, todas as ordens de proteção serão canceladas automaticamente.

## Como funciona

1. Assina os dados do Level1 para receber cotações de compra/venda atualizadas que são necessárias para as verificações de direção.
2. Mantém o controle do volume e da direção da posição da estratégia atual. Presume-se que as posições sejam criadas manualmente ou por outras estratégias.
3. Em cada atualização de cotação, posição ou negociação pessoal, recalcula a qual lado do mercado o `ZenPrice` pertence e constrói o tipo de ordem de proteção correspondente.
4. Normaliza o preço solicitado usando a etapa de preço do instrumento e arredonda o volume da ordem para os limites da bolsa antes de enviar qualquer coisa ao conector de negociação.
5. Usa `ReRegisterOrder` para modificar ordens de proteção já ativas em vez de cancelá-las, correspondendo ao comportamento da modificação no local de MetaTrader.

## Parâmetro

- **`ZenPrice`** – preço absoluto que deve ser usado como nível de stop-loss ou take-profit. Defina o valor como `0` para desativar a automação. Padrão: `0`.

## Notas práticas

- A estratégia nunca envia ordens de entrada. É seguro iniciá-lo junto com terminais de negociação discricionários ou outras estratégias automatizadas.
- As ordens de proteção são emitidas somente após o primeiro instantâneo de Nível 1 entregar cotações de compra e venda. Até então, o script espera, assim como a versão original do MQL dependia das aspas do terminal.
- Quando apenas um lado do mercado satisfaz a condição (por exemplo, `ZenPrice` está acima da oferta, mas não abaixo da oferta), a outra ordem de proteção é cancelada para evitar preços obsoletos.
- Todos os comentários dentro do código estão em inglês, enquanto esta documentação é fornecida em vários idiomas de acordo com as diretrizes do projeto.

## Diferenças do script MetaTrader

- O script original modifica os campos stop-loss e take-profit de um ticket de posição existente. StockSharp expõe ordens de proteção como ordens de stop e limite explícitas, portanto, a conversão opera em ordens visíveis na bolsa.
- MetaTrader ajusta automaticamente o preço de acordo com a precisão do corretor. Nesta porta, o mesmo comportamento é reproduzido via `NormalizePrice`, que aproveita a etapa de preço do símbolo e as configurações decimais.
- O volume de posições é arredondado para limites de lote de troca antes do envio das ordens de proteção, garantindo compatibilidade com locais que exigem etapas específicas de lote.
