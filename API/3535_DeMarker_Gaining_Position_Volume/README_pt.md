# Estratégia de volume de ganho de posição DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader *"DeMarker ganhando volume de posição"*. Ele usa o oscilador DeMarker para detectar extremos de sobrevenda e sobrecompra, acumulando gradualmente exposição quando o mercado permanece em uma condição esticada. A implementação opera em velas completadas e garante que apenas um sinal por barra seja processado.

A versão C# concentra-se na lógica discricionária central do script original enquanto adota o StockSharp API de alto nível. O gerenciamento de pedidos, o crescimento do volume e o comportamento de reversão opcional estão disponíveis por meio de parâmetros estratégicos, permitindo que o algoritmo seja adaptado a diferentes mercados e prazos.

## Parâmetros
- **Período DeMarker** – número de velas usadas pelo indicador DeMarker.
- **Nível superior** – limite do oscilador que prepara exposição curta (padrão `0.7`).
- **Nível inferior** – limite do oscilador que prepara exposição longa (padrão `0.3`).
- **Volume de negociação** – volume de ordens de mercado enviadas em cada sinal.
- **Apenas uma posição** – quando habilitada, a estratégia se estabiliza antes de abrir uma nova negociação para que a exposição líquida nunca misture posições longas e curtas.
- **Sinais Reversos** – troca os gatilhos de compra e venda, transformando a estratégia em uma versão contrária ou que segue a tendência.
- **Tipo de vela** – período de tempo das velas utilizadas para avaliação do indicador e sinal.

## Lógica de negociação
1. Uma assinatura de vela é aberta para o período selecionado e alimentada em um indicador DeMarker.
2. Quando a última vela finalizada fecha, o valor atual do DeMarker é comparado com os níveis configurados.
3. Sem reversão:
   - Se o DeMarker estiver abaixo do nível inferior, a estratégia tenta construir ou aumentar uma posição longa.
   - Se o DeMarker estiver acima do nível superior, a estratégia tenta construir ou aumentar uma posição curta.
4. Com a reversão habilitada, o significado dos níveis é invertido (mínimos extremos acionam posições vendidas e altas extremas acionam posições compradas).
5. O algoritmo lembra o horário da última negociação executada para evitar múltiplas entradas na mesma vela.

## Gerenciamento de posição
- Antes de mudar de direção, a estratégia verifica o lucro não realizado da posição existente. A exposição oposta é fechada somente se o preço atual da vela sair da negociação com um resultado positivo, refletindo o comportamento de proteção do EA original.
- As médias de posição são rastreadas internamente. Quando pedidos adicionais são adicionados na mesma direção, o preço médio é recalculado para avaliar corretamente a lucratividade.
- O parâmetro opcional *Only One Position* força um estado estável antes de entrar em uma nova negociação, o que é útil ao executar no modo de posição líquida.
- `StartProtection()` é invocado assim que a estratégia começa para garantir que a liquidação de emergência permaneça disponível se a posição se tornar diferente de zero e o algoritmo parar.

## Notas
- A conversão foi projetada para StockSharp API de alto nível e não depende de nenhuma coleção personalizada ou pesquisa direta de valor do indicador.
- Os modelos de dimensionamento de risco da versão MetaTrader (margem fixa, risco percentual, etc.) são intencionalmente simplificados para o parâmetro constante `Trade Volume`. Ajuste o dimensionamento da posição externamente se for necessário controle dinâmico de risco.
- Como os preenchimentos são simulados com ordens de mercado a preços de fechamento de velas, lembre-se de validar a configuração em relação à execução real do corretor e aos requisitos de deslizamento.
