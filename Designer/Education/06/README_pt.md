# Diagrama de Cubos Matemáticos e Fórmulas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este arquivo de esquema demonstra a utilização de cubos matemáticos e fórmulas da seção "Matemática" na ferramenta Designer, com foco específico em como empregar esses elementos em estratégias de trading.

## Visão Geral

O esquema explora o uso de fórmulas para tomar decisões de trading com base na comparação do preço de fechamento de um ativo com seus parâmetros estatísticos calculados via Simple Moving Average (SMA) e desvio padrão.

## Detalhes da Estratégia

- **Condição de Venda**: A estratégia emite uma ordem de venda se o preço de fechamento da vela anterior for maior que o valor SMA dos últimos 20 períodos mais três vezes o desvio padrão do mesmo período.
- **Condição de Compra**: Uma ordem de compra é executada se o preço de fechamento da vela anterior for menor que o valor SMA dos últimos 20 períodos menos três vezes o desvio padrão.

## Mudanças na Versão 5

- **Seção Matemática**: Na versão 5 do software Designer, a seção "Matemática" foi removida. Todos os cubos anteriormente encontrados nesta seção foram consolidados em um único cubo "Fórmula", simplificando o processo de design e implementação.
- **Cubo de Abertura de Posição**: O cubo "Abrir Posição" foi substituído pelo cubo "Registrar Ordem" na versão 5, refletindo mudanças em como as ordens são processadas dentro da plataforma.

Este esquema demonstra de forma eficaz como aproveitar cálculos matemáticos avançados para criar estratégias de trading dinâmicas e estatisticamente fundamentadas. A integração desses elementos em um esquema de trading pode melhorar significativamente os processos de tomada de decisão ao fundamentá-los em análise quantitativa.
