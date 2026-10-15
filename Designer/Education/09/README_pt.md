# Esquema de Estratégia Baseada em Tempo de Trabalho
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este arquivo de esquema demonstra a aplicação do bloco "Tempo de Trabalho" juntamente com outros blocos relevantes na plataforma Designer para implementar estratégias de negociação baseadas em tempo.

## Visão Geral

O esquema explora diversas configurações usando o bloco "Tempo de Trabalho", que permite aos traders executar estratégias com base em condições de tempo específicas.

## Componentes Principais

- **Bloco Tempo de Trabalho**: Usado para definir as horas de negociação ativas ou momentos específicos para executar trades.
- **Bloco Variável**: Denominado "Estratégia", este bloco é usado para armazenar e manipular variáveis específicas da estratégia.
- **Bloco Conversor**: Utilizado para converter e recuperar dados relacionados ao tempo para apoiar decisões baseadas em tempo.

## Detalhes da Estratégia

### Estratégia com Condição de Tempo de Trabalho
- **Compra Pré-Fechamento**: A estratégia inicia uma ordem de compra um minuto antes do encerramento do horário de trabalho definido, visando capitalizar possíveis movimentos de preço no final da sessão de negociação.

### Gatilho de Tempo Específico
- **Compra em Horário Fixo**: Implementa uma compra exatamente às 18:00, alinhando a execução do trade com eventos de mercado significativos ou horários típicos de fechamento.

### Encerramento Avançado Baseado em Tempo da Lição 7
- **Encerramento de Posições**: Fecha todas as posições abertas cinco minutos antes do fim do horário de trabalho — uma estratégia projetada para evitar manter posições overnight ou reagir às flutuações de preço no final do dia.

## Nota sobre as Alterações na Versão 5

Na quinta versão do software Designer, foram aprimorados os cálculos de tempo e o funcionamento conjunto do bloco "Tempo de Trabalho". Após importar estratégias que utilizam esses recursos, é recomendável recriá-las dentro da plataforma para garantir a funcionalidade correta e aproveitar as fórmulas de cálculo de tempo atualizadas.

Este esquema fornece uma estrutura abrangente para desenvolver e testar estratégias que dependem fortemente de precisão temporal para execução de trades, tornando-se uma ferramenta essencial para traders focados em estratégias intraday ou que precisam aderir a horários específicos de mercado.
