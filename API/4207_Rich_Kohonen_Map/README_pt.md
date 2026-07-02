# Estratégia do Mapa Rich Kohonen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia do Mapa Rich Kohonen é uma conversão do consultor especialista MetaTrader 4 "Rich.mq4". O sistema original constrói um mapa auto-organizado (rede Kohonen) sobre vetores de recursos derivados dos cálculos de pivô de Tom DeMark e classifica a próxima barra como uma oportunidade de compra, venda ou manutenção. A porta StockSharp preserva a abordagem de aprendizagem enquanto se integra à estratégia de alto nível API, operando exclusivamente em velas concluídas e ordens de mercado.

## Dados de mercado
- **Instrumento** – configurado por meio do `Security` vinculado no aplicativo host.
- **Tipo de vela** – parâmetro `CandleType` (padrão: período de 1 hora). A estratégia requer pelo menos sete velas finalizadas antes de produzir sinais, para que os vetores de características atuais e anteriores possam ser montados.

## Lógica de negociação
1. Mantenha uma janela contínua das últimas sete velas concluídas.
2. Construa dois vetores de sete elementos em cada vela acabada:
   - O **vetor atual** usa a abertura mais recente junto com as projeções de pivô de Tom DeMark calculadas a partir das cinco velas anteriores.
   - O **vetor anterior** desloca a janela em uma barra e representa a barra que acabou de fechar. Este vetor é usado para treinamento.
3. Compare o vetor atual com três mapas de Kohonen (comprar, vender, manter) e registre a distância euclidiana para cada unidade de melhor correspondência.
4. Selecione a ação com a menor distância e defina a posição alvo:
   - Comprar → exposição longa igual ao volume calculado.
   - Vender → exposição curta da mesma magnitude.
   - Segure → sem posição.
A estratégia envia ordens de mercado pela diferença entre a posição atual e a posição alvo para que a exposição final corresponda à decisão.
5. Calcule o movimento de abertura para abertura (em pips) entre as duas últimas velas e treine o mapa:
   - Movimento positivo dentro de `[MinPips, MaxPips]` → adicione o vetor anterior ao mapa de compra.
   - Movimento negativo dentro de `[-MaxPips, -MinPips]` → adicione o vetor anterior ao mapa de venda.
   - Caso contrário → armazene o vetor no mapa de espera.
6. O tamanho da posição é determinado dinamicamente a partir do saldo do portfólio: `floor(balance / 50) / 10`. Se isso produzir zero, o parâmetro substituto `Lots` será usado.

## Parâmetros
- `MinPips` – limite inferior (em pips) para considerar um movimento positivo de abertura para abertura como exemplo de treinamento de compra.
- `MaxPips` – limite superior (em pips) para amostras de treinamento de compra/venda.
- `TakeProfit`, `StopLoss` – preservado do especialista MQL para fins de documentação. A implementação de alto nível fecha ou reverte posições através de ordens de mercado, em vez de anexar stops.
- `Lots` – volume de fallback aplicado quando a fórmula baseada em saldo produz zero.
- `Slippage` – reservado para ajuste manual de pedidos (não usado diretamente pelos auxiliares API de alto nível).
- `MapPath` – caminho do arquivo binário usado para persistir os três mapas Kohonen entre as execuções.
- `EAName` – comentário opcional armazenado para referência.
- `CandleType` – assinatura de vela usada para extração de recursos.

## Armazenamento persistente de mapas
A estratégia armazena o mapa treinado em um arquivo binário definido por `MapPath` (padrão `rl.bin` dentro do diretório de trabalho). O arquivo contém as matrizes de compra, venda e manutenção sequencialmente. Na inicialização as matrizes são carregadas e a estratégia conta as linhas não vazias para retomar o treinamento do estado anterior. Os arquivos ausentes são ignorados, o que faz com que os mapas comecem a partir da memória preenchida com zero.

## Diferenças do especialista MQL original
- Os pedidos são emitidos por meio de ajudantes StockSharp (`BuyMarket` / `SellMarket`) e direcionam a exposição final desejada em vez de forçar um fechamento completo e uma reabertura em cada barra. Isso mantém o comportamento eficaz e reduz transações duplicadas no ambiente gerenciado.
- Os níveis de stop-loss e take-profit permanecem como parâmetros de documentação, mas não são registrados como ordens separadas. As saídas de posição ocorrem quando o classificador seleciona o lado oposto ou a ação de espera.
- A manipulação de arquivos usa auxiliares de E/S do .NET; o formato do mapa permanece compatível (valores de precisão dupla ordenados de forma idêntica).

## Notas de uso
- Certifique-se de que a segurança selecionada exponha um `PriceStep` válido para que as diferenças de pip sejam calculadas corretamente. Se o passo estiver faltando ou for zero, a estratégia volta para um passo unitário.
- Os mapas Kohonen podem crescer muito (até 10.000 entradas de compra/venda e 25.000 entradas de retenção). Mantenha o caminho padrão em um dispositivo de armazenamento com capacidade suficiente (~2,5 MB quando cheio).
- Como o algoritmo treina continuamente, executar a estratégia em dados históricos antes da implantação em tempo real ajuda a preencher o mapa com amostras representativas.
