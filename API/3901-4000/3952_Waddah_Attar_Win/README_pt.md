# Estratégia Waddah Attar Win Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Waddah Attar Win Grid Strategy** replica o consultor especialista MetaTrader 4 do script `MQL/8210`. Ele mantém continuamente uma escada simétrica de ordens com limite de compra e venda em torno da oferta/venda atual. Quando o preço oscila em direção ao nível de grade mais recente, a estratégia empilha automaticamente uma nova ordem pendente um passo adiante, aumentando opcionalmente o volume de cada ordem adicional. O lucro flutuante é monitorado a cada atualização da carteira de ofertas e, uma vez atingido o ganho patrimonial configurado, todas as posições e ordens de trabalho são fechadas simultaneamente.

## Como funciona

1. **Inicialização**
   - Assina atualizações do livro de pedidos para reagir instantaneamente às alterações de compra/venda.
   - Registra o valor atual do portfólio para usar como referência de patrimônio líquido de base.
   - Inicia o subsistema integrado de proteção contra riscos do StockSharp.
2. **Gerenciamento de linha de base**
   - Sempre que não existam ordens ativas e a posição líquida seja estável, o último valor da carteira passa a ser o novo saldo de referência. Isso reflete o consultor especialista original, que armazenou o saldo da conta corrente em cada tick.
3. **Colocação inicial da grade**
   - Assim que a negociação for permitida e nenhuma ordem estiver ativa, a estratégia coloca duas ordens pendentes:
     - Um limite de compra `Step Points` abaixo do preço de venda atual.
     - Um limite de venda `Step Points` acima do preço de oferta atual.
   - Ambos os pedidos usam o valor `First Volume`.
4. **Acumulando novos pedidos**
   - Quando o preço de venda se move dentro de cinco etapas de preço do último limite de compra, a estratégia coloca um novo limite de compra um passo abaixo do nível anterior.
   - Quando o preço de compra se move dentro de cinco etapas de preço do último limite de venda, a estratégia coloca um novo limite de venda um passo completo acima do nível anterior.
   - Cada nova ordem pendente aumenta o volume em `Increment Volume`, permitindo a pirâmide no estilo martingale, se desejado.
5. **Captura de lucro**
   - O lucro flutuante é calculado como a diferença entre o patrimônio atual da carteira e o saldo de referência armazenado.
   - Quando esse lucro exceder `Min Profit`, todas as ordens ativas serão canceladas e todas as posições abertas serão achatadas com uma única chamada de `CloseAll`.
   - O patrimônio da linha de base é atualizado, permitindo que a rede reinicie do zero.

## Características da estratégia

- **Dados de mercado**: operam exclusivamente com base em instantâneos da carteira de pedidos de nível 1 (melhor oferta/venda).
- **Tipos de pedidos**: utiliza apenas pedidos com limite; nenhuma parada ou entrada no mercado é gerada automaticamente.
- **Exposição**: pode manter posições compradas e vendidas simultaneamente em carteiras habilitadas para hedge.
- **Controle de risco**: carece de stop-loss rígidos; depende da meta de lucro flutuante e de regras de risco externo.
- **Reentrada**: após o nivelamento ou cancelamento manual de ordens, a grade inicial é recriada automaticamente na próxima vez que o loop de dados de mercado for executado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Step Points` | `120` | Distância entre níveis de grade consecutivos, expressa em pontos de preço (múltiplos de etapas de preço). |
| `First Volume` | `0.1` | Volume utilizado para o primeiro par de ordens pendentes. |
| `Increment Volume` | `0.0` | Volume adicional adicionado a cada pedido recém-empilhado; definido como zero para manter todos os pedidos do mesmo tamanho. |
| `Min Profit` | `450` | Lucro flutuante (na moeda da conta) necessário para fechar todas as posições abertas e ordens pendentes. |

## Notas e limitações

- Certifique-se de que o `PriceStep` do instrumento esteja configurado corretamente; a estratégia multiplica `Step Points` por `PriceStep` para derivar os preços reais.
- Como o algoritmo cancela e substitui ordens com frequência, os limites da corretora ou da bolsa nas contagens de ordens pendentes devem ser considerados.
- Não há proteção integrada contra saques – considere combinar a estratégia com gerenciamento de risco externo ou paradas em nível de portfólio.
- A rede pode expandir-se indefinidamente se os preços sofrerem tendências acentuadas sem atingir a meta de lucro; escolha `Increment Volume` com cuidado para controlar o uso da margem.

## Arquivos

- `CS/WaddahAttarWinGridStrategy.cs` — implementação em C# da lógica de negociação.
- `README.md` — esta documentação (inglês).
- `README_ru.md` — Tradução para russo com conteúdo idêntico.
- `README_zh.md` — Tradução chinesa com conteúdo idêntico.
