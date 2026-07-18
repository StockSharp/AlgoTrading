# Estratégia Lucky
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Lucky é um scalper de rompimento que monitora mudanças rápidas entre os melhores preços de bid e ask. Compra quando o preço ask salta para cima um número configurável de pips e vende quando o bid cai o mesmo valor. As posições são fechadas imediatamente assim que se tornam lucrativas ou se o preço se mover adversamente além de um limiar protetor.

## Dados e execução

- **Dados de mercado**: requer cotações de Nível 1 para acessar o fluxo de melhor bid e ask.
- **Tipos de ordens**: usa ordens de mercado para todas as entradas e saídas para reagir rapidamente a choques de cotização.
- **Modo de posição**: projetado para contas estilo hedge, mas funciona com contas de netting acumulando exposição líquida.

## Parâmetros

- **Shift points** – distância mínima em pips entre cotações consecutivas que aciona uma nova operação. Um valor mais alto filtra o ruído, enquanto um valor menor reage até a saltos mínimos.
- **Limit points** – movimento adverso máximo (em pips) tolerado antes de fechar forçosamente uma posição aberta. Também escala com o tamanho do tick do instrumento.
- **Reverse mode** – inverte a direção de trading. Quando habilitado, choques altistas do ask abrem vendidos e choques baixistas do bid abrem comprados.

## Lógica de trading

1. **Inicialização**
   - Converte os parâmetros baseados em pontos em distâncias de preço reais usando o tamanho de tick do instrumento.
   - Assina dados de Nível 1 e reinicia os buffers internos para os preços anteriores de bid e ask.
2. **Entrada**
   - Quando o ask aumenta pelo menos o shift configurado em relação ao ask anterior, a estratégia abre um comprado (ou vendido em modo reverso).
   - Quando o bid diminui pelo menos o shift em relação ao bid anterior, a estratégia abre um vendido (ou comprado em modo reverso).
3. **Dimensionamento de volume**
   - A quantidade de ordem padrão vem da propriedade `Volume` da estratégia.
   - Se o patrimônio do portfólio estiver disponível, emula a lógica do MetaTrader alocando aproximadamente `FreeMargin / 10.000`, arredondado para um lote decimal, garantindo que contas maiores negociem com tamanhos maiores.
4. **Saída**
   - Posições compradas fecham assim que o bid supera o preço de entrada médio ou o ask cai abaixo da entrada pelo limite configurado.
   - Posições vendidas fecham uma vez que o ask cai abaixo da entrada ou o bid sobe acima da entrada pelo limite.

## Notas e dicas de uso

- Funciona melhor em pares de FX altamente líquidos ou CFDs de índices com saltos de cotação notáveis.
- Combine com gerenciamento de risco adicional como stop-outs a nível de portfólio ao testar ao vivo.
- Ative **Reverse mode** para transformar o rompimento em uma estratégia fade sem modificar nenhum outro parâmetro.
- Como a estratégia reage a cada atualização de cotação que se qualifica, considere throttle dos dados recebidos ou aumentar o limiar de shift em feeds ruidosos.
